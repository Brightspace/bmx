using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

namespace D2L.Bmx.Okta;

internal class OktaAuthenticator {
	private readonly IConsolePrompter _consolePrompter;
	private readonly IOktaSessionStorage _sessionStorage;

	public OktaAuthenticator( IConsolePrompter consolePrompter, IOktaSessionStorage sessionStorage ) {
		_consolePrompter = consolePrompter;
		_sessionStorage = sessionStorage;
	}

	public async Task<OktaAuthenticatedState> AuthenticateAsync(
		string org,
		string user,
		bool nonInteractive,
		IOktaApi oktaApi
	) {

		oktaApi.SetOrganization( org );

		var cachedSession = await AuthenticateFromCacheAsync( org, user, oktaApi );
		if( cachedSession.SuccessfulAuthentication ) {
			return cachedSession;
		} else if( nonInteractive ) {
			throw new BmxException( "Authentication failed. No cached session" );
		}

		string password = _consolePrompter.PromptPassword();

		var authState = await oktaApi.AuthenticateAsync( new AuthenticateOptions( user, password ) )
			.ConfigureAwait( false );

		string? sessionToken = null;

		if( authState.OktaStateToken is not null ) {

			var mfaOptions = authState.MfaOptions;
			int selectedMfaIndex = _consolePrompter.PromptMfa( mfaOptions );
			var selectedMfa = mfaOptions[selectedMfaIndex - 1];
			string mfaInput = "";

			// TODO: Handle retry
			if( selectedMfa.Type == MfaType.Challenge ) {
				mfaInput = _consolePrompter.PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Sms ) {
				await oktaApi.IssueMfaChallengeAsync( authState, selectedMfaIndex - 1 );
				mfaInput = _consolePrompter.PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Question ) {
				mfaInput = _consolePrompter.PromptMfaInput( "Answer" );
			} else if( selectedMfa.Type == MfaType.Unknown ) {
				// Identical to Code based workflow for now (capture same behaviour as BMX current)
				_consolePrompter.PromptMfaInput( selectedMfa.Name );
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
				Console.Error.WriteLine( "No config file found. Your Okta session will not be cached. " +
				"Consider running `bmx configure` if you own this machine." );
			}
			return new( SuccessfulAuthentication: true, OktaSessionId: sessionResp.Id );
		}

		throw new BmxException( "Authentication Failed" );
	}

	private async Task<OktaAuthenticatedState> AuthenticateFromCacheAsync(
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

	private void CacheOktaSession( string userId, string org, string sessionId, DateTimeOffset expiresAt ) {
		var session = new OktaSessionCache( userId, org, sessionId, expiresAt );
		var existingSessions = ReadOktaSessionCacheFile();
		existingSessions.Add( session );

		_sessionStorage.SaveSessions( existingSessions );
	}

	private string? GetCachedOktaSessionId( string userId, string org ) {
		if( !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			return null;
		}

		var oktaSessions = ReadOktaSessionCacheFile();
		var session = oktaSessions.Find( session => session.UserId == userId && session.Org == org );
		return session?.SessionId;
	}

	private List<OktaSessionCache> ReadOktaSessionCacheFile() {
		var sourceCache = _sessionStorage.Sessions();
		var currTime = DateTimeOffset.Now;
		return sourceCache.Where( session => session.ExpiresAt > currTime ).ToList();
	}

}
