using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;

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

		if( await TryAuthenticateFromCacheAsync( org, user, _oktaApi ) ) {
			return _oktaApi;
		}
		if( nonInteractive ) {
			throw new BmxException( "Authentication failed. No cached session" );
		}

		string password = _consolePrompter.PromptPassword();

		var authnResponse = await _oktaApi.AuthenticateAsync( user, password );

		if( authnResponse is AuthenticateResponse.MfaRequired mfaInfo ) {
			OktaMfaFactor mfaFactor = _consolePrompter.SelectMfa( mfaInfo.Factors );

			if( !IsMfaFactorTypeSupported( mfaFactor.FactorType ) ) {
				throw new BmxException( "Selected MFA not supported by BMX." );
			}

			// TODO: Handle retry
			if( mfaFactor.FactorType is "sms" or "call" or "email" ) {
				await _oktaApi.IssueMfaChallengeAsync( mfaInfo.StateToken, mfaFactor.Id );
			}
			string mfaResponse = _consolePrompter.GetMfaResponse( mfaFactor.FactorType == "question" ? "Answer" : "PassCode" );
			authnResponse = await _oktaApi.VerifyMfaChallengeResponseAsync( mfaInfo.StateToken, mfaFactor.Id, mfaResponse );
		}

		if( authnResponse is AuthenticateResponse.Success successInfo ) {
			var sessionResp = await _oktaApi.CreateSessionAsync( successInfo.SessionToken );

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

	private async Task<bool> TryAuthenticateFromCacheAsync(
		string org,
		string user,
		IOktaApi oktaApi
	) {
		string? sessionId = GetCachedOktaSessionId( user, org );
		if( string.IsNullOrEmpty( sessionId ) ) {
			return false;
		}

		oktaApi.AddSession( sessionId );
		string? userId = await oktaApi.GetCurrentUserIdAsync( sessionId );
		return !string.IsNullOrEmpty( userId );
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
