using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D2L.Bmx;
internal class UpdateChecker {

	public static async Task CheckForUpdatesAsync( BmxConfig config ) {
		try {
			string? savedLatestVersion = GetSavedLatestVersion();
			var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
			var latestVersion = new Version( savedLatestVersion ?? "0.0.0" );
			if( ShouldFetchLatestVersion( savedLatestVersion ) ) {
				latestVersion = new Version( await GetLatestReleaseVersionAsync() );
			}

			string updateLocation = ( string.Equals( config.Org, "d2l", StringComparison.OrdinalIgnoreCase ) )
				? "https://bmx.d2l.dev"
				: "https://github.com/Brightspace/bmx/releases/latest";

			if( latestVersion > localVersion ) {
				DisplayUpdateMessage( $"A new BMX release is available: v{latestVersion} (current: v{localVersion})\n" +
					$"Upgrade now at {updateLocation}" );
			}
		} catch( Exception ) {
			// Give up and don't bother telling the user we couldn't check for updates
		}
	}

	private static void DisplayUpdateMessage( string message ) {
		var originalBackgroundColor = Console.BackgroundColor;
		var originalForegroundColor = Console.ForegroundColor;

		Console.BackgroundColor = ConsoleColor.Gray;
		Console.ForegroundColor = ConsoleColor.Black;

		string[] lines = message.Split( "\n" );
		int consoleWidth = Console.WindowWidth;

		foreach( string line in lines ) {
			Console.Error.Write( line.PadRight( consoleWidth ) );
			Console.Error.WriteLine();
		}

		Console.BackgroundColor = originalBackgroundColor;
		Console.ForegroundColor = originalForegroundColor;
		Console.ResetColor();
		Console.Error.WriteLine();
	}

	private static async Task<string> GetLatestReleaseVersionAsync() {
		using var httpClient = new HttpClient {
			BaseAddress = new Uri( "https://api.github.com" ),
			Timeout = TimeSpan.FromSeconds( 5 )
		};
		httpClient.DefaultRequestHeaders.Add( "User-Agent", "BMX" );
		var response = await httpClient.GetAsync( "repos/Brightspace/bmx/releases/latest" );
		response.EnsureSuccessStatusCode();

		using var responseStream = await response.Content.ReadAsStreamAsync();
		var releaseData = await JsonSerializer.DeserializeAsync(
			responseStream,
			SourceGenerationContext.Default.GithubRelease
		);
		string version = releaseData?.TagName?.Replace( "v", "" ) ?? string.Empty;
		SaveLatestVersion( version );
		return version;
	}

	private static void SaveLatestVersion( string version ) {
		if( string.IsNullOrWhiteSpace( version ) ) {
			return;
		}
		if( !Directory.Exists( BmxPaths.BMX_DIR ) ) {
			Directory.CreateDirectory( BmxPaths.BMX_DIR );
		}
		var op = new FileStreamOptions {
			Mode = FileMode.OpenOrCreate,
			Access = FileAccess.ReadWrite,
		};
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var fs = new FileStream( BmxPaths.UPDATE_CHECK_FILE_NAME, op );
		using var writer = new StreamWriter( fs );
		writer.WriteLine( version );
	}

	private static DateTimeOffset? GetTimeLastChecked() {
		if( !File.Exists( BmxPaths.UPDATE_CHECK_FILE_NAME ) ) {
			return null;
		}
		return File.GetLastWriteTimeUtc( BmxPaths.UPDATE_CHECK_FILE_NAME );
	}

	private static bool ShouldFetchLatestVersion( string? savedVersion ) {
		if( string.IsNullOrWhiteSpace( savedVersion ) ) {
			return true;
		}
		var savedTimestamp = GetTimeLastChecked();
		if( !savedTimestamp.HasValue || ( DateTimeOffset.UtcNow - savedTimestamp.Value ) > TimeSpan.FromDays( 1 ) ) {
			return true;
		}
		return false;
	}

	private static string? GetSavedLatestVersion() {
		if( !File.Exists( BmxPaths.UPDATE_CHECK_FILE_NAME ) ) {
			return null;
		}
		return File.ReadAllText( BmxPaths.UPDATE_CHECK_FILE_NAME );
	}
}

internal record GithubRelease {
	[JsonPropertyName( "tag_name" )]
	public string? TagName { get; set; }
}
