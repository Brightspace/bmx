using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal class OktaSessionStorage {

	internal static void SaveSessions( List<OktaSessionCache> sessions ) {

		string jsonString = JsonSerializer.Serialize(
			sessions,
			SourceGenerationContext.Default.ListOktaSessionCache );
		File.WriteAllText( BmxPaths.SESSIONS_FILE_NAME, jsonString );
	}

	internal static List<OktaSessionCache> Sessions() {

		var bmxDirectory = BmxPaths.BMX_DIR;
		var sessionsFileName = BmxPaths.SESSIONS_FILE_NAME;

		if( !Directory.Exists( bmxDirectory ) ) {
			Directory.CreateDirectory( bmxDirectory );
		}

		if( !File.Exists( sessionsFileName ) ) {
			File.WriteAllText( sessionsFileName, "[]" );
		}

		try {
			var sessionsJson = File.ReadAllText( sessionsFileName );
			var sessions = JsonSerializer.Deserialize( sessionsJson, SourceGenerationContext.Default.ListOktaSessionCache );
			if( sessions is not null ) {
				return sessions;
			}
		} catch( JsonException ) {
			return new();
		}
		return new();
	}
}
