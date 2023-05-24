using System.Runtime.InteropServices;
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
		WriteTextToFile( BmxPaths.SESSIONS_FILE_NAME, jsonString );
	}

	List<OktaSessionCache> IOktaSessionStorage.Sessions() {

		string bmxDirectory = BmxPaths.BMX_DIR;
		string sessionsFileName = BmxPaths.SESSIONS_FILE_NAME;

		if( !Directory.Exists( bmxDirectory ) ) {
			Directory.CreateDirectory( bmxDirectory );
		}

		if( !File.Exists( sessionsFileName ) ) {
			WriteTextToFile( sessionsFileName, "[]" );
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
	private void WriteTextToFile( string path, string Content ) {
		var op = new FileStreamOptions {
			Mode = FileMode.Create,
			Access = FileAccess.Write,
		};
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var writer = new StreamWriter( path, op );
		writer.Write( Content );
	}
}
