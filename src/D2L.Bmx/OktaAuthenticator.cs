using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;
using PuppeteerSharp;

namespace D2L.Bmx;

internal record OktaAuthenticatedContext(
	string Org,
	string User,
	IOktaAuthenticatedClient Client
);

internal class OktaAuthenticator(
	IOktaClientFactory oktaClientFactory,
	IOktaSessionStorage sessionStorage,
	IConsolePrompter consolePrompter,
	IConsoleWriter consoleWriter,
	BmxConfig config
) {
	public async Task<OktaAuthenticatedContext> AuthenticateAsync(
		string? org,
		string? user,
		bool nonInteractive,
		bool ignoreCache,
		bool experimental,
		bool? passwordless
	) {
		var orgSource = ParameterSource.CliArg;
		if( string.IsNullOrEmpty( org ) && !string.IsNullOrEmpty( config.Org ) ) {
			org = config.Org;
			orgSource = ParameterSource.Config;
		}
		if( string.IsNullOrEmpty( org ) ) {
			if( nonInteractive ) {
				throw new BmxException( "Org value was not provided" );
			}
			org = consolePrompter.PromptOrg( allowEmptyInput: false );
		} else if( !nonInteractive ) {
			consoleWriter.WriteParameter( ParameterDescriptions.Org, org, orgSource );
		}

		var userSource = ParameterSource.CliArg;
		if( string.IsNullOrEmpty( user ) && !string.IsNullOrEmpty( config.User ) ) {
			user = config.User;
			userSource = ParameterSource.Config;
		}
		if( string.IsNullOrEmpty( user ) ) {
			if( nonInteractive ) {
				throw new BmxException( "User value was not provided" );
			}
			user = consolePrompter.PromptUser( allowEmptyInput: false );
		} else if( !nonInteractive ) {
			consoleWriter.WriteParameter( ParameterDescriptions.User, user, userSource );
		}

		if( passwordless is null && config.Passwordless is not null ) {
			passwordless = config.Passwordless;
		}

		var oktaAnonymous = oktaClientFactory.CreateAnonymousClient( org );

		if( !ignoreCache && TryAuthenticateFromCache( org, user, oktaClientFactory, out var oktaAuthenticated ) ) {
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaAuthenticated );
		}
		if( passwordless == true
			&& await TryAuthenticateWithDSSOAsync( org, user, oktaClientFactory, experimental ) is { } oktaDSSOAuthenticated
		) {
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaDSSOAuthenticated );
		}
		if( nonInteractive ) {
			throw new BmxException( "Okta authentication failed. Please run `bmx login` first." );
		}

		string password = consolePrompter.PromptPassword();

		var authnResponse = await oktaAnonymous.AuthenticateAsync( user, password );

		if( authnResponse is AuthenticateResponse.Failure failure ) {
			throw new BmxException( $"""
				Okta authentication for user '{user}' in org '{org}' failed ({failure.StatusCode}).
				Check if org, user, and password is correct.
				""" );
		}

		if( authnResponse is AuthenticateResponse.MfaRequired mfaInfo ) {
			OktaMfaFactor mfaFactor = consolePrompter.SelectMfa( mfaInfo.Factors );

			if( mfaFactor.FactorName == OktaMfaFactor.UnsupportedMfaFactor ) {
				throw new BmxException( "Selected MFA not supported by BMX" );
			}

			// TODO: Handle retry
			if( mfaFactor.RequireChallengeIssue ) {
				await oktaAnonymous.IssueMfaChallengeAsync( mfaInfo.StateToken, mfaFactor.Id );
			}

			string mfaResponse = consolePrompter.GetMfaResponse(
				mfaFactor is OktaMfaQuestionFactor questionFactor ? questionFactor.Profile.QuestionText : "PassCode",
				mfaFactor is OktaMfaQuestionFactor // Security question factor is a static value
			);

			authnResponse = await oktaAnonymous.VerifyMfaChallengeResponseAsync( mfaInfo.StateToken, mfaFactor.Id, mfaResponse );
		}

		if( authnResponse is AuthenticateResponse.Success successInfo ) {
			var sessionResp = await oktaAnonymous.CreateSessionAsync( successInfo.SessionToken );

			oktaAuthenticated = oktaClientFactory.CreateAuthenticatedClient( org, sessionResp.Id );
			if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
				CacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			} else {
				consoleWriter.WriteWarning( """
					No config file found. Your Okta session will not be cached.
					Consider running `bmx configure` if you own this machine.
					""" );
			}
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaAuthenticated );
		}

		if( authnResponse is AuthenticateResponse.Failure failure2 ) {
			throw new BmxException( $"Error verifying MFA with Okta ({failure2.StatusCode})." );
		}

		throw new UnreachableException( $"Unexpected response type: {authnResponse.GetType()}" );
	}

	private bool TryAuthenticateFromCache(
		string org,
		string user,
		IOktaClientFactory oktaClientFactory,
		[NotNullWhen( true )] out IOktaAuthenticatedClient? oktaAuthenticated
	) {
		string? sessionId = GetCachedOktaSessionId( user, org );
		if( string.IsNullOrEmpty( sessionId ) ) {
			oktaAuthenticated = null;
			return false;
		}

		oktaAuthenticated = oktaClientFactory.CreateAuthenticatedClient( org, sessionId );
		return true;
	}

	private async Task<IOktaAuthenticatedClient?> TryAuthenticateWithDSSOAsync(
		string org,
		string user,
		IOktaClientFactory oktaClientFactory,
		bool experimental
	) {
		await using IBrowser? browser = await Browser.LaunchBrowserAsync( experimental );
		if( browser is null ) {
			return null;
		}

		Console.WriteLine( "Attempting to automatically login using DSSO." );
		string normalizedOrg = org.Replace( ".okta.com", "" );
		var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 15 ) );
		var sessionIdTaskProducer = new TaskCompletionSource<string?>( TaskCreationOptions.RunContinuationsAsynchronously );
		string? sessionId;

		try {
			var page = await browser.NewPageAsync().WaitAsync( cancellationTokenSource.Token );
			string baseAddress = $"https://{normalizedOrg}.okta.com/";
			int attempt = 1;

			page.Load += ( _, _ ) => _ = GetSessionCookieAsync();
			await page.GoToAsync( baseAddress );
			sessionId = await sessionIdTaskProducer.Task.WaitAsync( cancellationTokenSource.Token );

			async Task GetSessionCookieAsync() {
				var url = new Uri( page.Url );
				if( url.Host == $"{normalizedOrg}.okta.com" ) {
					string title = await page.GetTitleAsync();
					// DSSO can sometimes takes more than one attempt.
					// If the path is '/', it means DSSO is not available and we should stop retrying.
					if( title.Contains( "sign in", StringComparison.OrdinalIgnoreCase ) ) {
						if( attempt < 3 && url.AbsolutePath != "/" ) {
							attempt++;
							await page.GoToAsync( baseAddress );
						} else {
							sessionIdTaskProducer.SetResult( null );
						}
						return;
					}
				}
				var cookies = await page.GetCookiesAsync( baseAddress );
				if( Array.Find( cookies, c => c.Name == "sid" )?.Value is string sid ) {
					sessionIdTaskProducer.SetResult( sid );
				}
			}
		} catch( TaskCanceledException ) {
			consoleWriter.WriteWarning( $"""
				WARNING: Timed out when trying to create {normalizedOrg} Okta session through DSSO.
				Check if the org is correct. If running BMX with elevated privileges,
				rerun the command with the '--experimental-bypass-browser-security' flag
				"""
			);
			return null;
		} catch( TargetClosedException ) {
			consoleWriter.WriteWarning( $"""
				WARNING: Failed to create {normalizedOrg} Okta session through DSSO as BMX is likely being run
				with elevated privileges. Rerun the command with the '--experimental-bypass-browser-security' flag.
				"""
			);
			return null;
		} catch( Exception ) {
			consoleWriter.WriteWarning( "WARNING: Unknown error while trying to authenticate with Okta using DSSO." );
			return null;
		} finally {
			cancellationTokenSource.Dispose();
			browser.Dispose();
		}

		if( sessionId is null ) {
			return null;
		}

		var oktaAuthenticatedClient = oktaClientFactory.CreateAuthenticatedClient( org, sessionId );
		var sessionExpiry = ( await oktaAuthenticatedClient.GetSessionExpiryAsync() ).ExpiresAt;
		// We can expect a 404 if the session does not belong to the user which will throw an exception
		try {
			string userResponse = await oktaAuthenticatedClient.GetPageAsync( $"users/{user}" );
		} catch( Exception ) {
			consoleWriter.WriteWarning(
				$"WARNING: Failed to create {org} Okta session through DSSO as created session does not belong to {user}." );
			return null;
		}
		if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			CacheOktaSession( user, org, sessionId, sessionExpiry );
		} else {
			consoleWriter.WriteWarning( """
					No config file found. Your Okta session will not be cached.
					Consider running `bmx configure` if you own this machine.
					""" );
		}
		return oktaAuthenticatedClient;
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
