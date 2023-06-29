using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal class OktaAuthenticator(
	IOktaApi oktaApi,
	IOktaSessionStorage sessionStorage,
	IConsolePrompter consolePrompter,
	BmxConfig config
) {
	public async Task<IOktaApi> AuthenticateAsync(
		string? org,
		string? user,
		bool nonInteractive,
		bool ignoreCache
	) {
		if( string.IsNullOrEmpty( org ) ) {
			if( !string.IsNullOrEmpty( config.Org ) ) {
				org = config.Org;
			} else if( !nonInteractive ) {
				org = consolePrompter.PromptOrg( allowEmptyInput: false );
			} else {
				throw new BmxException( "Org value was not provided" );
			}
		}

		if( string.IsNullOrEmpty( user ) ) {
			if( !string.IsNullOrEmpty( config.User ) ) {
				user = config.User;
			} else if( !nonInteractive ) {
				user = consolePrompter.PromptUser( allowEmptyInput: false );
			} else {
				throw new BmxException( "User value was not provided" );
			}
		}

		oktaApi.SetOrganization( org );

		if( !ignoreCache && await TryAuthenticateFromCacheAsync( org, user, oktaApi ) ) {
			return oktaApi;
		}
		if( nonInteractive ) {
			throw new BmxException( "Authentication failed. No cached session" );
		}

		string password = consolePrompter.PromptPassword();

		var authnResponse = await oktaApi.AuthenticateAsync( user, password );

		if( authnResponse is AuthenticateResponse.MfaRequired mfaInfo ) {
			OktaMfaFactor mfaFactor = consolePrompter.SelectMfa( mfaInfo.Factors );

			if( !IsMfaFactorTypeSupported( mfaFactor.FactorType ) ) {
				throw new BmxException( "Selected MFA not supported by BMX." );
			}

			// TODO: Handle retry
			if( mfaFactor.FactorType is "sms" or "call" or "email" ) {
				await oktaApi.IssueMfaChallengeAsync( mfaInfo.StateToken, mfaFactor.Id );
			}
			string mfaResponse = consolePrompter.GetMfaResponse( mfaFactor.FactorType == "question" ? "Answer" : "PassCode" );
			authnResponse = await oktaApi.VerifyMfaChallengeResponseAsync( mfaInfo.StateToken, mfaFactor.Id, mfaResponse );
		}

		if( authnResponse is AuthenticateResponse.Success successInfo ) {
			var sessionResp = await oktaApi.CreateSessionAsync( successInfo.SessionToken );

			// TODO: Consider making OktaAPI stateless as well (?)
			oktaApi.AddSession( sessionResp.Id );
			if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
				CacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			} else {
				Console.Error.WriteLine( "No config file found. Your Okta session will not be cached. " +
				"Consider running `bmx configure` if you own this machine." );
			}
			return oktaApi;
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
		var sessionsToCache = ReadOktaSessionCacheFile();
		sessionsToCache = sessionsToCache.Where( session => session.UserId != userId && session.Org != org )
			.ToList();
		sessionsToCache.Add( session );

		sessionStorage.SaveSessions( sessionsToCache );
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
		var sourceCache = sessionStorage.GetSessions();
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
