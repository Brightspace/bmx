using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal interface IOktaSessionStorage {
	void SaveSessions( List<OktaSessionCache> sessions );
	List<OktaSessionCache> Sessions();
}

internal class OktaSessionStorage : IOktaSessionStorage {
	void IOktaSessionStorage.SaveSessions( List<OktaSessionCache> sessions ) {

		string jsonString = JsonSerializer.Serialize(
			sessions,
			SourceGenerationContext.Default.ListOktaSessionCache );
		File.WriteAllText( BmxPaths.SESSIONS_FILE_NAME, jsonString );
	}

	List<OktaSessionCache> IOktaSessionStorage.Sessions() {

		string bmxDirectory = BmxPaths.BMX_DIR;
		string sessionsFileName = BmxPaths.SESSIONS_FILE_NAME;

		if( !Directory.Exists( bmxDirectory ) ) {
			Directory.CreateDirectory( bmxDirectory );
		}

		if( !File.Exists( sessionsFileName ) ) {
			File.WriteAllText( sessionsFileName, "[]" );
		}

		try {
			string sessionsJson = File.ReadAllText( sessionsFileName );
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
