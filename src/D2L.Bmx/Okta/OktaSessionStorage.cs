using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal interface IOktaSessionStorage {
	void SaveSessions( List<OktaSessionCache> sessions );
	List<OktaSessionCache> GetSessions();
}

internal class OktaSessionStorage : IOktaSessionStorage {
	void IOktaSessionStorage.SaveSessions( List<OktaSessionCache> sessions ) {

		string jsonString = JsonSerializer.Serialize(
			sessions,
			JsonCamelCaseContext.Default.ListOktaSessionCache );
		WriteTextToFile( BmxPaths.SESSIONS_FILE_NAME, jsonString );
	}

	List<OktaSessionCache> IOktaSessionStorage.GetSessions() {
		if( !File.Exists( BmxPaths.SESSIONS_FILE_NAME ) ) {
			return new();
		}

		try {
			string sessionsJson = File.ReadAllText( BmxPaths.SESSIONS_FILE_NAME );
			return JsonSerializer.Deserialize( sessionsJson, JsonCamelCaseContext.Default.ListOktaSessionCache )
				?? new();
		} catch( JsonException ) {
			return new();
		}
	}

	private static void WriteTextToFile( string path, string content ) {
		var op = new FileStreamOptions {
			Mode = FileMode.Create,
			Access = FileAccess.Write,
		};
		if( !OperatingSystem.IsWindows() ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var writer = new StreamWriter( path, op );
		writer.Write( content );
	}
}
