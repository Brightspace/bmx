using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using D2L.Bmx.GitHub;

namespace D2L.Bmx;

internal class UpdateChecker( IGitHubClient github, IVersionProvider versionProvider, IConsoleWriter consoleWriter ) {
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

			// assembly version (System.Version) is easier to compare,
			// but informational version (string) is what users usually see (e.g. in GitHub & when running "bmx --version")
			Version? localVersion = versionProvider.GetAssemblyVersion();
			if( latestVersion > localVersion ) {
				string? displayVersion = versionProvider.GetInformationalVersion() ?? localVersion.ToString();
				consoleWriter.WriteUpdateMessage(
					$"""
					A new BMX release is available: v{latestVersion} (current: {displayVersion})
					Run "bmx update" now to upgrade.
					"""
				);
			}
		} catch( Exception ) {
			// Give up and don't bother telling the user we couldn't check for updates
		}
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
		if( !OperatingSystem.IsWindows() ) {
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
