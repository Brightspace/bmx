using System.Text.RegularExpressions;

namespace D2L.Bmx.E2eTests;

internal static partial class TestHelpers {
	private static readonly string BmxConfigDir = Path.Combine(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".bmx"
	);

	private static readonly string AwsConfigFile = Path.Combine(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".aws", "config"
	);

	private static readonly string AwsCredentialsFile = Path.Combine(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".aws", "credentials"
	);

	public static string BmxConfigFile => Path.Combine( BmxConfigDir, "config" );
	public static string BmxCacheDir => Path.Combine( BmxConfigDir, "cache" );
	public static string BmxSessionsFile => Path.Combine( BmxCacheDir, "sessions" );

	public static void RemoveBmxConfig() {
		if( Directory.Exists( BmxConfigDir ) ) {
			Directory.Delete( BmxConfigDir, recursive: true );
		}
	}

	public static void RemoveBmxConfigFileOnly() {
		if( File.Exists( BmxConfigFile ) ) {
			File.Delete( BmxConfigFile );
		}
	}

	public static void WriteLocalBmxFile( string directory, string content ) {
		File.WriteAllText( Path.Combine( directory, ".bmx" ), content );
	}

	public static void RemoveLocalBmxFile( string directory ) {
		string path = Path.Combine( directory, ".bmx" );
		if( File.Exists( path ) ) {
			File.Delete( path );
		}
	}

	// Removes [profile bmx-test*] from ~/.aws/config and [bmx-test*] from ~/.aws/credentials.
	public static void RemoveBmxTestProfiles() {
		RemoveMatchingProfiles(
			AwsConfigFile,
			BmxTestConfigProfileRegex()
		);
		RemoveMatchingProfiles(
			AwsCredentialsFile,
			BmxTestCredentialProfileRegex()
		);
	}

	public static string ReadBmxConfig() {
		return File.Exists( BmxConfigFile )
			? File.ReadAllText( BmxConfigFile )
			: string.Empty;
	}

	public static bool SessionsFileExists() {
		return File.Exists( BmxSessionsFile );
	}

	public static string ReadSessionsFile() {
		return File.Exists( BmxSessionsFile )
			? File.ReadAllText( BmxSessionsFile )
			: string.Empty;
	}

	public static string CreateTempWorkDir() {
		string dir = Path.Combine( Path.GetTempPath(), "bmx-e2e-tests", Guid.NewGuid().ToString( "N" ) );
		Directory.CreateDirectory( dir );
		return dir;
	}

	public static void CleanupTempWorkDir( string dir ) {
		if( Directory.Exists( dir ) ) {
			Directory.Delete( dir, recursive: true );
		}
	}

	private static void RemoveMatchingProfiles( string filePath, Regex sectionStart ) {
		if( !File.Exists( filePath ) ) return;

		string[] lines = File.ReadAllLines( filePath );
		var output = new List<string>();
		bool skipping = false;

		foreach( string line in lines ) {
			if( sectionStart.IsMatch( line ) ) {
				skipping = true;
			} else if( line.StartsWith( '[' ) ) {
				skipping = false;
			}

			if( !skipping ) {
				output.Add( line );
			}
		}

		File.WriteAllLines( filePath, output );
	}

	[GeneratedRegex( @"^\[profile bmx-test" )]
	private static partial Regex BmxTestConfigProfileRegex();

	[GeneratedRegex( @"^\[bmx-test" )]
	private static partial Regex BmxTestCredentialProfileRegex();
}
