using System.Formats.Tar;
using System.IO.Compression;
using D2L.Bmx.GitHub;

namespace D2L.Bmx;

internal class UpdateHandler( IGitHubClient github, IVersionProvider versionProvider ) {
	public async Task HandleAsync() {
		Console.WriteLine( "Finding the latest BMX release..." );
		GitHubRelease latestRelease;
		try {
			latestRelease = await github.GetLatestBmxReleaseAsync();
		} catch {
			throw new BmxException( "Failed to find the latest BMX release." );
		}
		Version latestVersion = latestRelease.Version
			?? throw new BmxException( "Failed to find the latest version of BMX." );

		var bmxFileInfo = GetFileInfo();

		// assembly version (System.Version) is easier to compare,
		// but informational version (string) is what users usually see (e.g. in GitHub & when running "bmx --version")
		Version? localVersion = versionProvider.GetAssemblyVersion();
		string? displayVersion = versionProvider.GetInformationalVersion() ?? localVersion?.ToString();
		if( latestVersion <= localVersion ) {
			Console.WriteLine( $"You already have the latest version {displayVersion}" );
			return;
		}
		var asset = latestRelease.Assets.Find( a => a.Name == bmxFileInfo.ArchiveName )
			?? throw new BmxException( "Failed to find the download URL of the latest BMX" );

		string workspaceDir = Path.Join( Path.GetTempPath(), Path.GetRandomFileName() );
		try {
			Directory.CreateDirectory( workspaceDir );
		} catch( Exception ex ) {
			throw new BmxException( "Failed to create a temporary directory for the update", ex );
		}

		Console.WriteLine( "Downloading the latest BMX..." );
		string downloadPath = Path.Join( workspaceDir, bmxFileInfo.ArchiveName );
		try {
			await github.DownloadAssetAsync( asset, downloadPath );
		} catch( Exception ex ) {
			throw new BmxException( "Failed to download the update", ex );
		}

		Console.WriteLine( "Extracting files from archive..." );
		string extractFolder = Path.Join( workspaceDir, "latest" );
		try {
			Directory.CreateDirectory( extractFolder );
			if( bmxFileInfo.ArchiveType == ArchiveType.Zip ) {
				ExtractZipFile( downloadPath, extractFolder );
			} else {
				ExtractTarGzipFile( downloadPath, extractFolder );
			}
		} catch( Exception ex ) {
			throw new BmxException( "Failed to extract from downloaded archive", ex );
		}

		Console.WriteLine( "Replacing the currently running BMX executable..." );
		string currentFilePath = Environment.ProcessPath
			?? throw new BmxException( "Failed to locate the current BMX executable." );
		string backupPath = Path.Join( workspaceDir, $"bmx-{displayVersion}-old.bak" );
		try {
			File.Move( currentFilePath, backupPath );
		} catch( UnauthorizedAccessException ex ) {
			throw new BmxException(
				$"""
				Permission denied when removing the old version ({currentFilePath}).
				Please try again with elevated permissions.
				""", ex );
		} catch( Exception ex ) {
			throw new BmxException( $"Failed to back up the old version ({currentFilePath})", ex );
		}
		string newFilePath = Path.Join( extractFolder, bmxFileInfo.ExeName );
		try {
			File.Move( newFilePath, currentFilePath );
		} catch( Exception ex ) {
			string errorMessage = "Failed to update with the new version.";
			try {
				File.Move( backupPath, currentFilePath );
			} catch {
				errorMessage += $"""

					Failed to restore the backup.
					Your BMX executable is now at {backupPath}. Please restore manually.
					""";
			}
			throw new BmxException( errorMessage, ex );
		}

		Console.WriteLine( $"BMX updated to v{latestVersion} successfully!" );

		try {
			Directory.Delete( workspaceDir, recursive: true );
		} catch {
			// Ignore any errors from clean-up.
			// This just isn't always possible (e.g. when on Windows), and it's harmless anyway.
		}
	}

	private static BmxFileInfo GetFileInfo() {
		if( OperatingSystem.IsWindows() ) {
			return new BmxFileInfo(
				ArchiveName: "bmx-win-x64.zip",
				ExeName: "bmx.exe",
				ArchiveType: ArchiveType.Zip
			);
		} else if( OperatingSystem.IsMacOS() ) {
			return new BmxFileInfo(
				ArchiveName: "bmx-osx-x64.tar.gz",
				ExeName: "bmx",
				ArchiveType: ArchiveType.TarGzip
			);
		} else if( OperatingSystem.IsLinux() ) {
			return new BmxFileInfo(
				ArchiveName: "bmx-linux-x64.tar.gz",
				ExeName: "bmx",
				ArchiveType: ArchiveType.TarGzip
			);
		} else {
			throw new BmxException(
				"Unable to choose the appropriate file for your current OS. Please update manually." );
		}
	}

	private static void ExtractTarGzipFile( string compressedFilePath, string decompressedFilePath ) {
		string tarPath = Path.Combine( decompressedFilePath, "bmx.tar" );
		using( FileStream compressedFileStream = File.Open(
			compressedFilePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read )
		) {
			using FileStream outputFileStream = File.Create( tarPath );
			using var decompressor = new GZipStream( compressedFileStream, CompressionMode.Decompress );
			decompressor.CopyTo( outputFileStream );
		}

		try {
			TarFile.ExtractToDirectory( tarPath, decompressedFilePath, overwriteFiles: true );
		} finally {
			File.Delete( tarPath );
		}
	}

	private static void ExtractZipFile( string compressedFilePath, string decompressedFilePath ) {
		using ZipArchive archive = ZipFile.OpenRead( compressedFilePath );
		foreach( ZipArchiveEntry entry in archive.Entries ) {
			string destinationPath = Path.GetFullPath( Path.Combine( decompressedFilePath, entry.FullName ) );
			if( destinationPath.StartsWith( decompressedFilePath, StringComparison.Ordinal ) ) {
				entry.ExtractToFile( destinationPath, overwrite: true );
			}
		}
	}


	private readonly record struct BmxFileInfo(
		string ArchiveName,
		string ExeName,
		ArchiveType ArchiveType
	);

	private enum ArchiveType {
		TarGzip,
		Zip,
	}
}
