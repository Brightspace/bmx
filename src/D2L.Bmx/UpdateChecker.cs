using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using D2L.Bmx.GitHub;

namespace D2L.Bmx;

internal class UpdateChecker( IGitHubClient github, BmxConfig config ) {
	public async Task CheckForUpdatesAsync() {
		try {
			var cachedVersion = GetUpdateCheckCache();
			var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
			var latestVersion = new Version( cachedVersion?.VersionName ?? "0.0.0" );
			if( ShouldFetchLatestVersion( cachedVersion ) ) {
				var releaseData = await github.GetLatestBmxReleaseAsync();
				latestVersion = releaseData.Version;
				if( latestVersion is null ) {
					return;
				}
				SaveVersion( latestVersion );
			}

			string updateLocation = string.Equals( config.Org, "d2l", StringComparison.OrdinalIgnoreCase )
				? "https://bmx.d2l.dev"
				: "https://github.com/Brightspace/bmx/releases/latest";

			if( latestVersion > localVersion ) {
				DisplayUpdateMessage(
					$"""
					A new BMX release is available: v{latestVersion} (current: v{localVersion})
					Upgrade now at {updateLocation}
					"""
				);
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

	private static void SaveVersion( Version version ) {
		var cache = new UpdateCheckCache {
			VersionName = version.ToString(),
			TimeLastChecked = DateTimeOffset.UtcNow
		};
		string content = JsonSerializer.Serialize( cache, JsonCamelCaseContext.Default.UpdateCheckCache );
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
			return JsonSerializer.Deserialize( content, JsonCamelCaseContext.Default.UpdateCheckCache );
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


internal record UpdateCheckCache {
	public string? VersionName { get; set; }
	public DateTimeOffset? TimeLastChecked { get; set; }
}
