using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace D2L.Bmx;

internal class UpdateHandler {

	public async Task HandleAsync() {
		using var httpClient = new HttpClient();
		httpClient.BaseAddress = new Uri( "https://api.github.com" );
		httpClient.Timeout = TimeSpan.FromSeconds( 2 );
		httpClient.DefaultRequestHeaders.Add( "User-Agent", "BMX" );
		var response = await httpClient.GetAsync( "repos/Brightspace/bmx/releases/latest" );
		response.EnsureSuccessStatusCode();

		await using var responseStream = await response.Content.ReadAsStreamAsync();
		var releaseData = await JsonSerializer.DeserializeAsync(
			responseStream,
			SourceGenerationContext.Default.GithubRelease
		);
		string archiveName = GetOSFileName();
		string downloadUrl = releaseData?.Assets?.FirstOrDefault( a => a.Name == archiveName )?.BrowserDownloadUrl
			?? string.Empty;
		string? currentProcessPath = Environment.ProcessPath;
		string backupPath = currentProcessPath + ".old.bak"; // will figure out how to do version later
		if( string.IsNullOrWhiteSpace( downloadUrl ) ) {
			return;
		}

		if( !string.IsNullOrEmpty( currentProcessPath ) ) {
			File.Move( currentProcessPath, backupPath );
		} else {
			currentProcessPath = "C:/bin";
		}

		var archiveRes = await httpClient.GetAsync( downloadUrl, HttpCompletionOption.ResponseHeadersRead );
		string? downloadPath = Path.GetTempFileName();
		using( var fs = new FileStream( downloadPath, FileMode.Create, FileAccess.Write, FileShare.None ) ) {
			await archiveRes.Content.CopyToAsync( fs );
			await fs.FlushAsync();
			fs.Dispose();
		}
		Console.WriteLine( "Downloaded!" );

		string extension = Path.GetExtension( downloadUrl ).ToLowerInvariant();
		string? extractPath = Path.GetDirectoryName( currentProcessPath );

		if( extension.Equals( ".zip" ) ) {
			using( ZipArchive archive = ZipFile.OpenRead( downloadPath ) ) {
				foreach( ZipArchiveEntry entry in archive.Entries ) {
					string? destinationPath = Path.GetFullPath( Path.Combine( extractPath!, entry.FullName ) );
					if( destinationPath.StartsWith( extractPath!, StringComparison.Ordinal ) ) {
						entry.ExtractToFile( destinationPath, overwrite: true );
					}
				}
			}
		}

		string newExecutablePath = Path.Combine( extractPath!, Path.GetFileName( currentProcessPath ) );
		File.Move( newExecutablePath, currentProcessPath, overwrite: true );
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
		string processDirectory = Path.GetDirectoryName( Environment.ProcessPath ) ?? string.Empty;
		if( string.IsNullOrEmpty( processDirectory ) ) {
			return;
		}

		Console.WriteLine( $"Cleaning up old binaries in {processDirectory}" );
		foreach( string file in Directory.GetFiles( processDirectory, "*.old.bak" ) ) {
			try {
				Console.WriteLine( file );
				File.Delete( file );
			} catch( Exception ex ) {
				Console.WriteLine( $"Failed to delete old binary {file}: {ex.Message}" );
			}
		}
	}
}
