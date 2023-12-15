using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace D2L.Bmx;

internal class UpdateHandler {

	public async Task HandleAsync() {
		using var httpClient = new HttpClient();
		var releaseData = await UpdateChecker.GetLatestReleaseDataAsync();
		var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
		var latestVersion = new Version( UpdateChecker.GetLatestReleaseVersion( releaseData ) );
		if( latestVersion <= localVersion ) {
			Console.WriteLine( $"Already own the latest version {latestVersion}" );
			return;
		}
		string archiveName = GetOSFileName();
		string downloadUrl = releaseData?.Assets?.FirstOrDefault( a => a.Name == archiveName )?.BrowserDownloadUrl
			?? string.Empty;
		if( string.IsNullOrWhiteSpace( downloadUrl ) ) {
			throw new BmxException( "Failed to get update download url" );
		}

		string? downloadPath = Path.GetTempFileName();

		string currentFilePath = Process.GetCurrentProcess().MainModule!.FileName;
		string currentDirectory = Path.GetDirectoryName( currentFilePath )!;
		string backupPath = $"{BmxPaths.OLD_BMX_VERSION_FILE_NAME}v{localVersion}.old.bak";

		try {
			Console.Error.WriteLine( currentFilePath );
			Console.Error.WriteLine( backupPath );
			File.Move( currentFilePath, backupPath );
		} catch( IOException ex ) {
			throw new BmxException( "Could move the old version, try with elevated permissions", ex );
		} catch {
			throw new Exception( "Could not get current process path" );
		}

		try {
			var archiveRes = await httpClient.GetAsync( downloadUrl, HttpCompletionOption.ResponseHeadersRead );
			using( var fs = new FileStream( downloadPath, FileMode.Create, FileAccess.Write, FileShare.None ) ) {
				await archiveRes.Content.CopyToAsync( fs );
				await fs.FlushAsync();
				fs.Dispose();
			}
		} catch( Exception ex ) {
			File.Move( backupPath, currentFilePath );
			throw new BmxException( "Failed to download the update", ex );
		}

		try {
			string extension = Path.GetExtension( downloadUrl ).ToLowerInvariant();

			if( extension.Equals( ".zip" ) ) {
				ExtractZipFile( downloadPath, currentDirectory );
			} else if( extension.Equals( ".gz" ) ) {
				ExtractTarGzipFile( downloadPath, currentDirectory );
			} else {
				throw new Exception( "Unknown archive type" );
			}
		} catch( Exception ex ) {
			File.Move( backupPath, currentFilePath );
			throw new BmxException( "Failed to update with new files", ex );
		} finally {
			File.Delete( downloadPath );
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
			throw new Exception( "Unknown OS" );
		}
	}

	public static void Cleanup() {
		string processDirectory = Path.GetDirectoryName( BmxPaths.OLD_BMX_VERSION_FILE_NAME ) ?? string.Empty;
		if( string.IsNullOrEmpty( processDirectory ) ) {
			return;
		}
		foreach( string file in Directory.GetFiles( processDirectory, "*.old.bak" ) ) {
			try {
				File.Delete( file );
			} catch( Exception ex ) {
				Console.Error.WriteLine( $"WARNING: Failed to delete old version {file}" );
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
		using( ZipArchive archive = ZipFile.OpenRead( compressedFilePath ) ) {
			foreach( ZipArchiveEntry entry in archive.Entries ) {
				string destinationPath = Path.GetFullPath( Path.Combine( decompressedFilePath!, entry.FullName ) );
				if( destinationPath.StartsWith( decompressedFilePath!, StringComparison.Ordinal ) ) {
					entry.ExtractToFile( destinationPath, overwrite: true );
				}
			}
		}
	}
}
