using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace D2L.Bmx;

internal class UpdateHandler {

	public async Task HandleAsync() {
		if( !Directory.Exists( BmxPaths.OLD_BMX_VERSIONS_PATH ) ) {
			try {
				Directory.CreateDirectory( BmxPaths.OLD_BMX_VERSIONS_PATH );
			} catch( Exception ex ) {
				throw new BmxException( "Failed to initialize temporary BMX file directory (~/.bmx/temp)", ex );
			}
		}

		using var httpClient = new HttpClient();
		GithubRelease? releaseData = await GithubRelease.GetLatestReleaseDataAsync();
		Version? latestVersion = releaseData?.GetReleaseVersion();
		if( latestVersion is null ) {
			throw new BmxException( "Failed to find the latest version of BMX." );
		}

		var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
		if( latestVersion <= localVersion ) {
			Console.WriteLine( $"You already have the latest version {latestVersion}" );
			return;
		}

		string archiveName = GetOSFileName();
		string? downloadUrl = releaseData?.Assets?.FirstOrDefault( a => a.Name == archiveName )?.BrowserDownloadUrl;
		if( string.IsNullOrWhiteSpace( downloadUrl ) ) {
			throw new BmxException( "Failed to find the download URL of the latest BMX" );
		}

		string? currentFilePath = Environment.ProcessPath;
		if( string.IsNullOrEmpty( currentFilePath ) ) {
			throw new BmxException( "BMX could not update" );
		}

		string downloadPath = Path.GetTempFileName();
		try {
			var archiveRes = await httpClient.GetAsync( downloadUrl, HttpCompletionOption.ResponseHeadersRead );
			using var fs = new FileStream( downloadPath, FileMode.Create, FileAccess.Write, FileShare.None );
			await archiveRes.Content.CopyToAsync( fs );
			await fs.FlushAsync();
		} catch( Exception ex ) {
			throw new BmxException( "Failed to download the update", ex );
		}

		string extractFolder = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
		try {
			Directory.CreateDirectory( extractFolder );
		} catch( Exception ex ) {
			File.Delete( downloadPath );
			throw new BmxException( "Failed to initialize temporary folder for downloaded file", ex );
		}

		string currentDirectory = Path.GetDirectoryName( currentFilePath )!;
		long backupPathTimeStamp = DateTime.Now.Millisecond;
		string backupPath = Path.Join( BmxPaths.OLD_BMX_VERSIONS_PATH, $"bmx-v{localVersion}-{backupPathTimeStamp}-old.bak" );
		try {
			string extension = Path.GetExtension( downloadUrl );

			if( extension.Equals( ".zip", StringComparison.OrdinalIgnoreCase ) ) {
				ExtractZipFile( downloadPath, extractFolder );
			} else if( extension.Equals( ".gz", StringComparison.OrdinalIgnoreCase ) ) {
				ExtractTarGzipFile( downloadPath, extractFolder );
			} else {
				throw new Exception( "Unknown archive type" );
			}
		} catch( Exception ex ) {
			Directory.Delete( extractFolder, recursive: true );
			throw new BmxException( "Failed to update with new files", ex );
		} finally {
			File.Delete( downloadPath );
		}

		try {
			File.Move( currentFilePath, backupPath );
		} catch( IOException ex ) {
			Directory.Delete( extractFolder, recursive: true );
			throw new BmxException( "Could not remove the old version. Please try again with elevated permissions.", ex );
		} catch {
			Directory.Delete( extractFolder, recursive: true );
			throw new BmxException( "BMX could not update" );
		}

		try {
			foreach( string file in Directory.GetFiles( extractFolder ) ) {
				string destinationFile = Path.Combine( currentDirectory, Path.GetFileName( file ) );
				File.Move( file, destinationFile );
			}
		} catch( Exception ex ) {
			File.Move( backupPath, currentFilePath );
			throw new BmxException( "BMX could not update with the new version", ex );
		} finally {
			Directory.Delete( extractFolder, recursive: true );
		}
	}

	private static string GetOSFileName() {

		if( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			return "bmx-win-x64.zip";
		} else if( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) ) {
			return "bmx-osx-x64.tar.gz";
		} else if( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
			return "bmx-linux-x64.tar.gz";
		} else {
			throw new BmxException( "New version does not support you current OS" );
		}
	}

	public static void Cleanup() {
		if( Directory.Exists( BmxPaths.OLD_BMX_VERSIONS_PATH ) ) {
			try {
				Directory.Delete( BmxPaths.OLD_BMX_VERSIONS_PATH, recursive: true );
			} catch( Exception ) {
				Console.Error.WriteLine( "WARNING: Failed to delete old version files" );
			}
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
			TarFile.ExtractToDirectory( tarPath, decompressedFilePath, true );
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
}
