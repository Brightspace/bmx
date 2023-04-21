using System.Text.Json;
using System.Text.RegularExpressions;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal class OktaSessionStorage {

	internal static void SaveSessions( List<OktaSessionCache> sessions ) {

		string jsonString = JsonSerializer.Serialize(
			sessions.ToArray(),
			SourceGenerationContext.Default.OktaSessionCacheArray );
		File.WriteAllText( SessionsFileName(), jsonString );
	}

	internal static List<OktaSessionCache> Sessions() {

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
			var sessions = JsonSerializer.Deserialize( sessionsJson, SourceGenerationContext.Default.OktaSessionCacheArray );
			if( sessions is not null ) {
				return sessions.ToList();
			}
		} catch( JsonException ) {
			return new();
		}
		return new();
	}

	internal static string SessionsFileName() {
		return Path.Join( UserHomeDir(), ".bmx3", "sessions" );
	}

	internal static string BmxDir() {
		return Path.Join( UserHomeDir(), ".bmx3" );
	}

	internal static string UserHomeDir() {

		string osPlatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
		string pattern = @"(?i)\bwindows\b";

		if( Regex.IsMatch( osPlatform, pattern ) ) {
			string? home = Environment.GetEnvironmentVariable( "HOMEDRIVE" ) + Environment.GetEnvironmentVariable( "HOMEPATH" );
			if( home is null || home.Equals( "" ) ) {
				home = Environment.GetEnvironmentVariable( "USERPROFILE" );
			}
			return home!;
		}
		return Environment.GetEnvironmentVariable( "HOME" )!;
	}
}
