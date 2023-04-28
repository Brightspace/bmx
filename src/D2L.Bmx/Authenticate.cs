using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

namespace D2L.Bmx;

internal class Authenticator {
	async public static Task<OktaAuthenticatedState> AuthenticateAsync(
		string org,
		string user,
		bool nomask,
		IOktaApi oktaApi ) {

		oktaApi.SetOrganization( org );

		var cachedSession = await AuthenticateFromCacheAsync( org, user, oktaApi );
		if( cachedSession.SuccessfulAuthentication ) {
			return cachedSession;
		}

		Console.Write( "Okta Password: " );
		var password = "";

		// TODO: remove nomask
		while( true ) {
			var input = Console.ReadKey( intercept: true );
			if( input.Key == ConsoleKey.Enter ) {
				Console.Write( '\n' );
				break;
			} else if( input.Key == ConsoleKey.Backspace && password.Length > 0 ) {
				password = password.Remove( password.Length - 1, 1 );
			} else if( !char.IsControl( input.KeyChar ) ) {
				password += input.KeyChar;
			}
		}

		var authState = await oktaApi.AuthenticateOktaAsync( new AuthenticateOptions( user, password ) )
			.ConfigureAwait( false );

		string? sessionToken = null;

		if( authState.OktaStateToken is not null ) {

			var mfaOptions = authState.MfaOptions;
			var selectedMfaIndex = PromptMfa( mfaOptions );
			var selectedMfa = mfaOptions[selectedMfaIndex - 1];
			string mfaInput = "";

			// TODO: Handle retry
			if( selectedMfa.Type == MfaType.Challenge ) {
				mfaInput = PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Sms ) {
				await oktaApi.IssueMfaChallengeOktaAsync( authState, selectedMfaIndex - 1 );
				mfaInput = PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Question ) {
				mfaInput = PromptMfaInput( "Answer" );
			} else if( selectedMfa.Type == MfaType.Unknown ) {
				// Identical to Code based workflow for now (capture same behaviour as BMX current)
				PromptMfaInput( selectedMfa.Name );
				throw new NotImplementedException();
			}
			sessionToken = await oktaApi.AuthenticateChallengeMfaOktaAsync( authState, selectedMfaIndex - 1, mfaInput );
		} else if( authState.OktaSessionToken is not null ) {
			sessionToken = authState.OktaSessionToken;
		}

		if( !string.IsNullOrEmpty( sessionToken ) ) {
			var sessionResp = await oktaApi.CreateSessionOktaAsync( new SessionOptions( sessionToken ) );
			// TODO: Consider making OktaAPI stateless as well (?)
			oktaApi.AddSession( sessionResp.Id );
			CacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			return new( SuccessfulAuthentication: true, OktaSessionId: sessionResp.Id );
		}

		throw new BmxException( "Authentication Failed" );
	}

	public static async Task<OktaAuthenticatedState> AuthenticateFromCacheAsync(
		string org,
		string user,
		IOktaApi oktaApi
	) {
		var sessionId = GetCachedOktaSession( user, org );
		if( string.IsNullOrEmpty( sessionId ) ) {
			return new( SuccessfulAuthentication: false, OktaSessionId: "" );
		}

		oktaApi.AddSession( sessionId );
		var userId = await oktaApi.GetMeResponseAsync( sessionId );
		if( !string.IsNullOrEmpty( userId ) ) {
			return new( SuccessfulAuthentication: true, OktaSessionId: userId );
		}
		return new( SuccessfulAuthentication: false, OktaSessionId: "" );
	}

	private static void CacheOktaSession( string userId, string org, string sessionId, DateTimeOffset expiresAt ) {
		var session = new OktaSessionCache( userId, org, sessionId, expiresAt );
		var existingSessions = ReadOktaSessionCacheFile();
		existingSessions.Add( session );

		OktaSessionStorage.SaveSessions( existingSessions );
	}

	private static string GetCachedOktaSession( string userId, string org ) {
		var oktaSessions = ReadOktaSessionCacheFile();
		var session = oktaSessions.Find( session => session.UserId == userId && session.Org == org );
		return session?.SessionId ?? "";
	}

	private static List<OktaSessionCache> ReadOktaSessionCacheFile() {
		var sourceCache = OktaSessionStorage.Sessions();
		var currTime = DateTimeOffset.Now;
		return sourceCache.Where( session => session.ExpiresAt > currTime ).ToList();
	}

	private static int PromptMfa( MfaOption[] mfaOptions ) {

		Console.WriteLine( "MFA Required" );
		for( int i = 0; i < mfaOptions.Length; i++ ) {
			Console.WriteLine( $"[{i + 1}] {mfaOptions[i].Provider}: {mfaOptions[i].Name}" );
		}
		Console.Write( "Select an available MFA option: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
			throw new BmxException( "Invalid account selection" );
		}

		return index;
	}

	private static string PromptMfaInput( string mfaInputPrompt ) {
		Console.Write( $"{mfaInputPrompt}: " );
		string? mfaInput = Console.ReadLine();

		if( mfaInput is not null ) {
			return mfaInput;
		}
		throw new BmxException( "Invalid Mfa Input" );
	}

}
