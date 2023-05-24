using System.Runtime.InteropServices;
using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal interface IOktaSessionStorage {
	void SaveSessions( List<OktaSessionCache> sessions );
	List<OktaSessionCache> Sessions();
}

internal class OktaSessionStorage : IOktaSessionStorage {

	//Update permission to 600 when a new entry is added
	void WriteFile600( string path, string Content ) {

		try {
			if( File.Exists( path ) ) {
				File.Delete( path );
			}
		} catch( IOException IOException ) {
			//maybe we cannot delete it somehow
		}
		var op = new FileStreamOptions();
		op.Mode = FileMode.Create; //Append false, overrite old
		op.Access = FileAccess.ReadWrite;
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.SetUser | UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var writer = new StreamWriter( path, op );
		writer.Write( Content );
	}
	void IOktaSessionStorage.SaveSessions( List<OktaSessionCache> sessions ) {
		string jsonString = JsonSerializer.Serialize(
			sessions,
			SourceGenerationContext.Default.ListOktaSessionCache );
		WriteFile600( BmxPaths.SESSIONS_FILE_NAME, jsonString );
	}

	List<OktaSessionCache> IOktaSessionStorage.Sessions() {

		string bmxDirectory = BmxPaths.BMX_DIR;
		string sessionsFileName = BmxPaths.SESSIONS_FILE_NAME;

		if( !Directory.Exists( bmxDirectory ) ) {
			Directory.CreateDirectory( bmxDirectory );
		}

		if( !File.Exists( sessionsFileName ) ) {
			WriteFile600( sessionsFileName, "[]" );
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
