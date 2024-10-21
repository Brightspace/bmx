using System.Diagnostics.CodeAnalysis;
using PuppeteerSharp;

namespace D2L.Bmx;

public static class Browser {

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
	private static readonly string[] MacPaths = [
		"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
		"/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
	];
	private static readonly string[] LinuxPaths = [
		"/opt/google/chrome/chrome",
		"/opt/microsoft/msedge/msedge",
	];

	public static async Task<IBrowser> LaunchBrowserAsync( string browserPath ) {
		var launchOptions = new LaunchOptions {
			ExecutablePath = browserPath,
			// For whatever reason, with an elevated user, Chromium cannot launch in headless mode without --no-sandbox.
			// This isn't a big concern for BMX, because it only visits the org Okta homepage, which should be trusted.
			Args = UserPrivileges.HasElevatedPermissions() ? ["--no-sandbox"] : []
		};

		return await Puppeteer.LaunchAsync( launchOptions );
	}

	public static bool TryGetPathToBrowser( [NotNullWhen( returnValue: true )] out string? path ) {
		path = null;
		if( OperatingSystem.IsWindows() ) {
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
		} else if( OperatingSystem.IsMacOS() ) {
			path = Array.Find( MacPaths, File.Exists );
			return path is not null;
		} else if( OperatingSystem.IsLinux() ) {
			path = Array.Find( LinuxPaths, File.Exists );
			return path is not null;
		}
		return false;
	}
}
