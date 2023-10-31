using System.Runtime.InteropServices;
using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal interface IOktaSessionStorage {
	void SaveSessions( List<OktaSessionCache> sessions );
	List<OktaSessionCache> GetSessions();
}

internal class OktaSessionStorage : IOktaSessionStorage {
	void IOktaSessionStorage.SaveSessions( List<OktaSessionCache> sessions ) {
		if( !Directory.Exists( BmxPaths.BMX_DIR ) ) {
			Directory.CreateDirectory( BmxPaths.BMX_DIR );
		}

		string jsonString = JsonSerializer.Serialize(
			sessions,
			SourceGenerationContext.Default.ListOktaSessionCache );
		WriteTextToFile( BmxPaths.SESSIONS_FILE_NAME, jsonString );
	}

	List<OktaSessionCache> IOktaSessionStorage.GetSessions() {
		if( !File.Exists( BmxPaths.SESSIONS_FILE_NAME ) ) {
			return new();
		}

		try {
			string sessionsJson = File.ReadAllText( BmxPaths.SESSIONS_FILE_NAME );
			return JsonSerializer.Deserialize( sessionsJson, SourceGenerationContext.Default.ListOktaSessionCache )
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
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var writer = new StreamWriter( path, op );
		writer.Write( content );
	}
}
