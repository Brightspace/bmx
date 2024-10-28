using System.Diagnostics.CodeAnalysis;
using PuppeteerSharp;

namespace D2L.Bmx;

internal interface IBrowserLauncher {
	Task<IBrowser> LaunchAsync( string browserPath );
	bool TryGetPathToBrowser( [NotNullWhen( returnValue: true )] out string? path );
}

internal class BrowserLauncher : IBrowserLauncher {

	// https://github.com/microsoft/playwright/blob/6763d5ab6bd20f1f0fc879537855a26c7644a496/packages/playwright-core/src/server/registry/index.ts#L630
	private static readonly string[] WindowsEnvironmentVariables = [
		"LOCALAPPDATA",
		"PROGRAMFILES",
		"PROGRAMFILES(X86)",
	];

	// https://github.com/microsoft/playwright/blob/6763d5ab6bd20f1f0fc879537855a26c7644a496/packages/playwright-core/src/server/registry/index.ts#L457-L459
	private static readonly string[] WindowsPartialPaths = [
		"Microsoft\\Edge\\Application\\msedge.exe",
		"Google\\Chrome\\Application\\chrome.exe",
	];

	async Task<IBrowser> IBrowserLauncher.LaunchAsync( string browserPath ) {
		var launchOptions = new LaunchOptions {
			ExecutablePath = browserPath,
			// For whatever reason, with an elevated user, Chromium cannot launch in headless mode without --no-sandbox.
			// This isn't a big concern for BMX, because it only visits the org Okta homepage, which should be trusted.
			Args = UserPrivileges.HasElevatedPermissions() ? ["--no-sandbox"] : []
		};

		return await Puppeteer.LaunchAsync( launchOptions );
	}

	bool IBrowserLauncher.TryGetPathToBrowser( [NotNullWhen( returnValue: true )] out string? path ) {
		path = null;
		if( !OperatingSystem.IsWindows() ) {
			return false;
		}

		foreach( string windowsPartialPath in WindowsPartialPaths ) {
			foreach( string environmentVariable in WindowsEnvironmentVariables ) {
				string? prefix = Environment.GetEnvironmentVariable( environmentVariable );
				if( prefix is not null ) {
					path = Path.Join( prefix, windowsPartialPath );
					if( File.Exists( path ) ) {
						return true;
					}
				}
			}
		}

		/* We only support passwordless auth on Windows at this point, because
		- Okta only supports DSSO on Windows and Mac, so Linux is out.
		- D2L hasn't configured DSSO for Mac, so we can't test it on macOS either.
		*/
		path = null;
		return false;
	}
}
