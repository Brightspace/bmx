using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace D2L.Bmx;

internal static class UpdateChecker {
	public static async Task CheckForUpdatesAsync( BmxConfig config ) {
		try {
			var cachedVersion = GetUpdateCheckCache();
			var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
			var latestVersion = new Version( cachedVersion?.VersionName ?? "0.0.0" );
			if( ShouldFetchLatestVersion( cachedVersion ) ) {
				latestVersion = new Version( await GetLatestReleaseVersionAsync() );
			}

			string updateLocation = string.Equals( config.Org, "d2l", StringComparison.OrdinalIgnoreCase )
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
		string version = releaseData?.TagName?.TrimStart( 'v' ) ?? string.Empty;
		SaveLatestVersion( version );
		return version;
	}

	private static void SaveLatestVersion( string version ) {
		if( string.IsNullOrWhiteSpace( version ) ) {
			return;
		}
		var cache = new UpdateCheckCache {
			VersionName = version,
			TimeLastChecked = DateTimeOffset.UtcNow
		};
		string content = JsonSerializer.Serialize( cache, SourceGenerationContext.Default.UpdateCheckCache );
		var op = new FileStreamOptions {
			Mode = FileMode.OpenOrCreate,
			Access = FileAccess.ReadWrite,
		};
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var fs = new FileStream( BmxPaths.UPDATE_CHECK_FILE_NAME, op );
		using var writer = new StreamWriter( fs );
		writer.Write( content );
	}

	private static UpdateCheckCache? GetUpdateCheckCache() {
		if( !File.Exists( BmxPaths.UPDATE_CHECK_FILE_NAME ) ) {
			return null;
		}

		string content = File.ReadAllText( BmxPaths.UPDATE_CHECK_FILE_NAME );
		try {
			return JsonSerializer.Deserialize( content, SourceGenerationContext.Default.UpdateCheckCache );
		} catch( JsonException ) {
			return null;
		}
	}

	private static bool ShouldFetchLatestVersion( UpdateCheckCache? cache ) {
		if( cache is null || string.IsNullOrWhiteSpace( cache.VersionName )
			|| ( DateTimeOffset.UtcNow - cache.TimeLastChecked ) > TimeSpan.FromDays( 1 )
			|| ( cache.TimeLastChecked > DateTimeOffset.UtcNow )
		) {
			return true;
		}
		return false;
	}
}

internal record GithubRelease {
	[JsonPropertyName( "tag_name" )]
	public string? TagName { get; set; }

	[JsonPropertyName( "assets" )]
	public List<Assets>? Assets { get; set; }
}

internal record Assets {
	[JsonPropertyName( "url" )]
	public string? Url { get; set; }

	[JsonPropertyName( "name" )]
	public string? Name { get; set; }

	[JsonPropertyName( "browser_download_url" )]
	public string? BrowserDownloadUrl { get; set; }
}

internal record UpdateCheckCache {
	public string? VersionName { get; set; }
	public DateTimeOffset? TimeLastChecked { get; set; }
}
