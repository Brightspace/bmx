using PuppeteerSharp;

namespace D2L.Bmx;

public class Browser {

	// https://github.com/microsoft/playwright/blob/6763d5ab6bd20f1f0fc879537855a26c7644a496/packages/playwright-core/src/server/registry/index.ts#L630
	private static readonly string[] WindowsEnvironmentVariables = [
		"LOCALAPPDATA",
		"PROGRAMFILES",
		"PROGRAMFILES(X86)",
	];

	// https://github.com/microsoft/playwright/blob/6763d5ab6bd20f1f0fc879537855a26c7644a496/packages/playwright-core/src/server/registry/index.ts#L457-L459
	private static readonly string[] WindowsPartialPaths = [
		"\\Microsoft\\Edge\\Application\\msedge.exe",
		"\\Google\\Chrome\\Application\\chrome.exe"
	];
	private static readonly string[] MacPaths = [
		"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
		"/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge"
	];
	private static readonly string[] LinuxPaths = [
		"/opt/google/chrome/chrome",
		"/opt/microsoft/msedge/msedge"
	];

	public static async Task<IBrowser?> LaunchBrowserAsync( bool noSandbox = false ) {
		string? browserPath = GetPathToBrowser();
		if( browserPath is null ) {
			return null;
		}

		var launchOptions = new LaunchOptions {
			Headless = true,
			ExecutablePath = browserPath,
			Args = noSandbox ? ["--no-sandbox"] : []
		};

		return await Puppeteer.LaunchAsync( launchOptions );
	}

	private static string? GetPathToBrowser() {
		string? browser = null;
		if( OperatingSystem.IsWindows() ) {
			foreach( string windowsPartialPath in WindowsPartialPaths ) {
				foreach( string environmentVariable in WindowsEnvironmentVariables ) {
					string? prefix = Environment.GetEnvironmentVariable( environmentVariable );
					if( prefix is not null ) {
						string path = prefix + windowsPartialPath;
						if( File.Exists( path ) ) {
							return path;
						}
					}
				}
			}
		} else if( OperatingSystem.IsMacOS() ) {
			return MacPaths.First( File.Exists );
		} else if( OperatingSystem.IsLinux() ) {
			return LinuxPaths.First( File.Exists );
		}
		return browser;
	}
}
