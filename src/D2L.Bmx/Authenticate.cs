using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

namespace D2L.Bmx;

internal class Authenticator {
	async public static Task<OktaAuthenticatedState> AuthenticateAsync(
		string org,
		string user,
		bool nomask,
		bool nonInteractive,
		IOktaApi oktaApi,
		string defaultMfaProvider,
		string defaultMfamethod ) {

		oktaApi.SetOrganization( org );

		var cachedSession = await AuthenticateFromCacheAsync( org, user, oktaApi );
		if( cachedSession.SuccessfulAuthentication ) {
			return cachedSession;
		} else if( nonInteractive ) {
			throw new BmxException( "Authentication failed. No cached session" );
		}

		Console.Write( "Okta Password: " );
		string password = "";

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

		var authState = await oktaApi.AuthenticateAsync( new AuthenticateOptions( user, password ) )
			.ConfigureAwait( false );

		string? sessionToken = null;

		if( authState.OktaStateToken is not null ) {

			var mfaOptions = authState.MfaOptions;
			int selectedMfaIndex = PromptMfa( mfaOptions, defaultMfaProvider, defaultMfamethod );
			var selectedMfa = mfaOptions[selectedMfaIndex - 1];
			string mfaInput = "";

			// TODO: Handle retry
			if( selectedMfa.Type == MfaType.Challenge ) {
				mfaInput = PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Sms ) {
				await oktaApi.IssueMfaChallengeAsync( authState, selectedMfaIndex - 1 );
				mfaInput = PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Question ) {
				mfaInput = PromptMfaInput( "Answer" );
			} else if( selectedMfa.Type == MfaType.Unknown ) {
				// Identical to Code based workflow for now (capture same behaviour as BMX current)
				PromptMfaInput( selectedMfa.Name );
				throw new NotImplementedException();
			}
			sessionToken = await oktaApi.AuthenticateChallengeMfaAsync( authState, selectedMfaIndex - 1, mfaInput );
		} else if( authState.OktaSessionToken is not null ) {
			sessionToken = authState.OktaSessionToken;
		}

		if( !string.IsNullOrEmpty( sessionToken ) ) {
			var sessionResp = await oktaApi.CreateSessionAsync( new SessionOptions( sessionToken ) );
			// TODO: Consider making OktaAPI stateless as well (?)
			oktaApi.AddSession( sessionResp.Id );
			if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
				CacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			} else {
				Console.WriteLine( "No config file found. Your Okta session will not be cached. " +
				"Consider running `bmx configure` if you own this machine." );
			}
			return new( SuccessfulAuthentication: true, OktaSessionId: sessionResp.Id );
		}

		throw new BmxException( "Authentication Failed" );
	}

	public static async Task<OktaAuthenticatedState> AuthenticateFromCacheAsync(
		string org,
		string user,
		IOktaApi oktaApi
	) {
		string? sessionId = GetCachedOktaSessionId( user, org );
		if( string.IsNullOrEmpty( sessionId ) ) {
			return new( SuccessfulAuthentication: false, OktaSessionId: "" );
		}

		oktaApi.AddSession( sessionId );
		string? userId = await oktaApi.GetMeResponseAsync( sessionId );
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

	private static string? GetCachedOktaSessionId( string userId, string org ) {
		if( !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			return null;
		}

		var oktaSessions = ReadOktaSessionCacheFile();
		var session = oktaSessions.Find( session => session.UserId == userId && session.Org == org );
		return session?.SessionId;
	}

	private static List<OktaSessionCache> ReadOktaSessionCacheFile() {
		var sourceCache = OktaSessionStorage.Sessions();
		var currTime = DateTimeOffset.Now;
		return sourceCache.Where( session => session.ExpiresAt > currTime ).ToList();
	}
	private static int MatchdefaultMfaoption(
		MfaOption[] mfaOptions,
		string defaultMfaProvider,
		string defaultMfamethod ) {
		return Array.IndexOf( mfaOptions,
			Array.Find( mfaOptions, option =>
				option.Provider == defaultMfaProvider && option.Name == defaultMfamethod
)
		);
	}
	private static int PromptMfa( MfaOption[] mfaOptions, string defaultMfaProvider, string defaultMfamethod ) {
		Console.WriteLine( "MFA Required" );
		int Defaultchoice = MatchdefaultMfaoption( mfaOptions, defaultMfaProvider, defaultMfamethod );
		if( Defaultchoice != -1 ) {
			Console.WriteLine(
				$"Using Default MFA method: {mfaOptions[Defaultchoice].Provider}: {mfaOptions[Defaultchoice].Name}"
			);
			return Defaultchoice + 1;
		}
		if( mfaOptions.Length > 1 ) {
			for( int i = 0; i < mfaOptions.Length; i++ ) {
				Console.WriteLine( $"[{i + 1}] {mfaOptions[i].Provider}: {mfaOptions[i].Name}" );
			}
			Console.Write( "Select an available MFA option: " );
			if( !int.TryParse( Console.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
				throw new BmxException( "Invalid account selection" );
			}
			return index;
		} else if( mfaOptions.Length == 0 ) {//idk, is mfaOptions' length gaurenteed to be >= 1?
			throw new BmxException( "No MFA method have been set up for the current user." );
		} else {
			Console.WriteLine( $"MFA method: {mfaOptions[0].Provider}: {mfaOptions[0].Name}" );
			return 1;
		}

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
