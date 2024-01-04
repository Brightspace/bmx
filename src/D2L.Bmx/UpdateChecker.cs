using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using D2L.Bmx.GitHub;

namespace D2L.Bmx;

internal class UpdateChecker( IGitHubClient github ) {
	public async Task CheckForUpdatesAsync() {
		try {
			var updateCheckCache = GetUpdateCheckCacheOrNull();

			Version? latestVersion;
			if( ShouldFetchLatestVersion( updateCheckCache ) ) {
				latestVersion = ( await github.GetLatestBmxReleaseAsync() ).Version;
				if( latestVersion is null ) {
					return;
				}
				SetUpdateCheckCache( latestVersion );
			} else {
				latestVersion = updateCheckCache.VersionName;
				if( latestVersion is null ) {
					return;
				}
			}

			Version? localVersion = Assembly.GetExecutingAssembly().GetName().Version;
			if( latestVersion > localVersion ) {
				DisplayUpdateMessage(
					$"""
					A new BMX release is available: v{latestVersion} (current: v{localVersion})
					Run "bmx update" now to upgrade.
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

	private static void SetUpdateCheckCache( Version version ) {
		var cache = new UpdateCheckCache(
			VersionName: version,
			TimeLastChecked: DateTimeOffset.UtcNow
		);
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

	private static UpdateCheckCache? GetUpdateCheckCacheOrNull() {
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

	private static bool ShouldFetchLatestVersion(
		[NotNullWhen( returnValue: false )] UpdateCheckCache? cache
	) {
		return cache?.VersionName is null
			|| cache.TimeLastChecked is null
			|| DateTimeOffset.UtcNow - cache.TimeLastChecked > TimeSpan.FromDays( 1 )
			|| cache.TimeLastChecked > DateTimeOffset.UtcNow;
	}
}


internal record UpdateCheckCache(
	Version? VersionName,
	DateTimeOffset? TimeLastChecked
);
