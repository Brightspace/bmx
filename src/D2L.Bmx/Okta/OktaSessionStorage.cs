using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal class OktaSessionStorage {

	internal static void SaveSessions( List<OktaSessionCache> sessions ) {

		if( !ConfigFileExists() ) {
			Console.WriteLine( "No config file found. Your Okta session will not be cached. " +
				"Consider running `bmx configure` if you own this machine." );
			return;
		}

		string jsonString = JsonSerializer.Serialize(
			sessions,
			SourceGenerationContext.Default.ListOktaSessionCache );
		File.WriteAllText( SessionsFileName(), jsonString );
	}

	internal static List<OktaSessionCache> Sessions() {

		if( !ConfigFileExists() ) {
			return new();
		}

		var bmxDirectory = BmxDir();
		var sessionsFileName = SessionsFileName();

		if( !Directory.Exists( bmxDirectory ) ) {
			Directory.CreateDirectory( bmxDirectory );
		}

		if( !File.Exists( sessionsFileName ) ) {
			File.WriteAllText( sessionsFileName, "[]" );
		}

		try {
			var sessionsJson = File.ReadAllText( SessionsFileName() );
			var sessions = JsonSerializer.Deserialize( sessionsJson, SourceGenerationContext.Default.ListOktaSessionCache );
			if( sessions is not null ) {
				return sessions;
			}
		} catch( JsonException ) {
			return new();
		}
		return new();
	}

	internal static string SessionsFileName() {
		return Path.Join( UserHomeDir(), ".bmx", "sessions" );
	}

	internal static string BmxDir() {
		return Path.Join( UserHomeDir(), ".bmx" );
	}

	internal static bool ConfigFileExists() {
		return File.Exists( Path.Join( UserHomeDir(), ".bmx", "config" ) );
	}

	internal static string UserHomeDir() {
		return Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
	}
}
