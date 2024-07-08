using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal record AuthenticatedOktaApi(
	string Org,
	string User,
	IOktaApi Api
);

internal class OktaAuthenticator(
	IOktaApi oktaApi,
	IOktaSessionStorage sessionStorage,
	IConsolePrompter consolePrompter,
	BmxConfig config
) {
	public async Task<AuthenticatedOktaApi> AuthenticateAsync(
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

		if( !ignoreCache && TryAuthenticateFromCache( org, user, oktaApi ) ) {
			return new AuthenticatedOktaApi( Org: org, User: user, Api: oktaApi );
		}
		if( nonInteractive ) {
			throw new BmxException( "Okta authentication failed. Please run `bmx login` first." );
		}

		string password = consolePrompter.PromptPassword( user, org );

		var authnResponse = await oktaApi.AuthenticateAsync( user, password );

		if( authnResponse is AuthenticateResponse.MfaRequired mfaInfo ) {
			UnsupportedOktaMfaFactor mfaFactor = consolePrompter.SelectMfa( mfaInfo.Factors );

			if (mfaFactor.GetType() == typeof(UnsupportedOktaMfaFactor)) {
				throw new BmxException( "Selected MFA not supported by BMX" );
			}

			// TODO: Handle retry
			if( mfaFactor.RequireChallengeIssue ) {
				await oktaApi.IssueMfaChallengeAsync( mfaInfo.StateToken, mfaFactor.Id );
			}

			string mfaResponse = consolePrompter.GetMfaResponse(
				mfaFactor is UnsupportedOktaMfaQuestionFactor questionFactor ? questionFactor.Profile.QuestionText : "PassCode",
				mfaFactor is UnsupportedOktaMfaQuestionFactor // Security question factor is a static value
			);

			authnResponse = await oktaApi.VerifyMfaChallengeResponseAsync( mfaInfo.StateToken, mfaFactor.Id, mfaResponse );
		}

		if( authnResponse is AuthenticateResponse.Success successInfo ) {
			var sessionResp = await oktaApi.CreateSessionAsync( successInfo.SessionToken );

			// TODO: Consider making OktaAPI stateless as well (?)
			oktaApi.AddSession( sessionResp.Id );
			if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
				CacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			} else {
				Console.ResetColor();
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Error.WriteLine( "No config file found. Your Okta session will not be cached. " +
				"Consider running `bmx configure` if you own this machine." );
				Console.ResetColor();
			}
			return new AuthenticatedOktaApi( Org: org, User: user, Api: oktaApi );
		}

		throw new BmxException( "Okta authentication failed" );
	}

	private bool TryAuthenticateFromCache(
		string org,
		string user,
		IOktaApi oktaApi
	) {
		string? sessionId = GetCachedOktaSessionId( user, org );
		if( string.IsNullOrEmpty( sessionId ) ) {
			return false;
		}

		oktaApi.AddSession( sessionId );
		return true;
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
}
