using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

namespace D2L.Bmx;

internal class OktaAuthenticator {
	private readonly IOktaApi _oktaApi;
	private readonly IOktaSessionStorage _sessionStorage;
	private readonly IConsolePrompter _consolePrompter;
	private readonly BmxConfig _config;

	public OktaAuthenticator(
		IOktaApi oktaApi,
		IOktaSessionStorage sessionStorage,
		IConsolePrompter consolePrompter,
		BmxConfig config
	) {
		_oktaApi = oktaApi;
		_sessionStorage = sessionStorage;
		_consolePrompter = consolePrompter;
		_config = config;
	}

	public async Task<IOktaApi> AuthenticateAsync(
		string? org,
		string? user,
		bool nonInteractive
	) {
		if( string.IsNullOrEmpty( org ) ) {
			if( !string.IsNullOrEmpty( _config.Org ) ) {
				org = _config.Org;
			} else if( !nonInteractive ) {
				org = _consolePrompter.PromptOrg();
			} else {
				throw new BmxException( "Org value was not provided" );
			}
		}

		if( string.IsNullOrEmpty( user ) ) {
			if( !string.IsNullOrEmpty( _config.User ) ) {
				user = _config.User;
			} else if( !nonInteractive ) {
				user = _consolePrompter.PromptUser();
			} else {
				throw new BmxException( "User value was not provided" );
			}
		}

		_oktaApi.SetOrganization( org );

		var cachedSession = await AuthenticateFromCacheAsync( org, user, _oktaApi );
		if( cachedSession.SuccessfulAuthentication ) {
			return _oktaApi;
		} else if( nonInteractive ) {
			throw new BmxException( "Authentication failed. No cached session" );
		}

		string password = _consolePrompter.PromptPassword();

		var authState = await _oktaApi.AuthenticateAsync( new AuthenticateOptions( user, password ) );

		string? sessionToken = null;

		if( authState.OktaStateToken is not null ) {
			OktaMfaFactor mfaFactor = _consolePrompter.SelectMfa( authState.OktaMfaFactors );

			if( !IsMfaFactorTypeSupported( mfaFactor.FactorType ) ) {
				throw new BmxException( "Selected MFA not supported by BMX." );
			}

			// TODO: Handle retry
			if( mfaFactor.FactorType is "sms" or "call" or "email" ) {
				await _oktaApi.IssueMfaChallengeAsync( authState, mfaFactor );
			}
			string mfaResponse = _consolePrompter.GetMfaResponse( mfaFactor.FactorType == "question" ? "Answer" : "PassCode" );
			sessionToken = await _oktaApi.VerifyMfaChallengeResponseAsync( authState, mfaFactor, mfaResponse );
		} else if( authState.OktaSessionToken is not null ) {
			sessionToken = authState.OktaSessionToken;
		}

		if( !string.IsNullOrEmpty( sessionToken ) ) {
			var sessionResp = await _oktaApi.CreateSessionAsync( new SessionOptions( sessionToken ) );
			// TODO: Consider making OktaAPI stateless as well (?)
			_oktaApi.AddSession( sessionResp.Id );
			if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
				CacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			} else {
				Console.Error.WriteLine( "No config file found. Your Okta session will not be cached. " +
				"Consider running `bmx configure` if you own this machine." );
			}
			return _oktaApi;
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
		var sourceCache = _sessionStorage.GetSessions();
		var currTime = DateTimeOffset.Now;
		return sourceCache.Where( session => session.ExpiresAt > currTime ).ToList();
	}

	private static bool IsMfaFactorTypeSupported( string mfaFactorType ) {
		return mfaFactorType is
			"call"
			or "email"
			or "question"
			or "sms"
			or "token:hardware"
			or "token:hotp"
			or "token:software:totp"
			or "token";
	}

}
