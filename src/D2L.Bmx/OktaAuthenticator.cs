using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal record OktaAuthenticatedContext(
	string Org,
	string User,
	IOktaAuthenticatedClient Client
);

internal class OktaAuthenticator(
	IOktaClientFactory oktaClientFactory,
	IOktaSessionStorage sessionStorage,
	IBrowserLauncher browserLauncher,
	IConsolePrompter consolePrompter,
	IMessageWriter messageWriter,
	BmxConfig config
) {
	public async Task<OktaAuthenticatedContext> AuthenticateAsync(
		string? org,
		string? user,
		bool nonInteractive,
		bool ignoreCache,
		int? passwordlessTimeout
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
			messageWriter.WriteParameter( ParameterDescriptions.Org, org, orgSource );
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
			messageWriter.WriteParameter( ParameterDescriptions.User, user, userSource );
		}

		var orgUrl = GetOrgBaseAddress( org );

		if( !ignoreCache && TryAuthenticateFromCache( orgUrl, user, out var oktaAuthenticated ) ) {
			return new( Org: org, User: user, Client: oktaAuthenticated );
		}

		if(
			// `TryGetPathToBrowser` only returns `true` for Windows, but restricting to Windows earlier here helps
			// the compiler trim more unused code (e.g. all of PuppeteerSharp)
			OperatingSystem.IsWindows()
			&& browserLauncher.TryGetPathToBrowser( out string? browserPath )
		) {
			int resolvedTimeout = passwordlessTimeout
				?? config.PasswordlessTimeout
				?? 30;

			if( resolvedTimeout == 0 ) {
				if( BmxEnvironment.IsDebug ) {
					messageWriter.WriteWarning( "Okta passwordless authentication disabled via configuration" );
				}
			} else {
				if( !nonInteractive ) {
					Console.Error.WriteLine( "Attempting Okta passwordless authentication..." );
				}
				oktaAuthenticated = await GetDssoAuthenticatedClientAsync(
					orgUrl,
					user,
					browserPath,
					resolvedTimeout
				);
				if( oktaAuthenticated is not null ) {
					return new( Org: org, User: user, Client: oktaAuthenticated );
				}
				if( !nonInteractive ) {
					Console.Error.WriteLine( "Falling back to Okta password authentication..." );
				}
			}
		} else if( BmxEnvironment.IsDebug ) {
			messageWriter.WriteWarning( "No suitable browser found for Okta passwordless authentication" );
		}

		if( nonInteractive ) {
			throw new BmxException( "Okta authentication failed. Please run `bmx login` first." );
		}

		oktaAuthenticated = await GetPasswordAuthenticatedClientAsync( orgUrl, user );
		return new( Org: org, User: user, Client: oktaAuthenticated );
	}

	private static Uri GetOrgBaseAddress( string org ) {
		return org.Contains( '.' )
			? new Uri( $"https://{org}/" )
			: new Uri( $"https://{org}.okta.com/" );
	}

	private bool TryAuthenticateFromCache(
		Uri orgBaseAddress,
		string user,
		[NotNullWhen( true )] out IOktaAuthenticatedClient? oktaAuthenticated
	) {
		OktaSessionCache? session = GetCachedOktaSession( user, orgBaseAddress.Host );
		if( session is null ) {
			oktaAuthenticated = null;
			return false;
		}

		oktaAuthenticated = oktaClientFactory.CreateAuthenticatedClient(
			orgBaseAddress,
			session.SessionId,
			session.SessionCookieName ?? OktaSessionCookieNames.Classic
		);
		return true;
	}

	private async Task<IOktaAuthenticatedClient?> GetDssoAuthenticatedClientAsync(
		Uri orgUrl,
		string user,
		string browserPath,
		int timeoutSeconds
	) {
		(string Name, string Value)? sessionCookie = null;

		try {
			sessionCookie = await GetSessionCookieFromBrowserAsync( browserPath, orgUrl, timeoutSeconds );
		} catch( TaskCanceledException ex ) {
			if( BmxEnvironment.IsDebug ) {
				messageWriter.WriteWarning( $"Okta passwordless authentication timed out. \n{ex}" );
			}
		} catch( Exception ex ) {
			if( BmxEnvironment.IsDebug ) {
				messageWriter.WriteWarning( $"Unknown error occurred while trying Okta passwordless authentication. \n{ex}" );
			}
		}

		if( sessionCookie is null ) {
			return null;
		}
		(string sessionCookieName, string sessionId) = sessionCookie.Value;

		var oktaAuthenticatedClient = oktaClientFactory.CreateAuthenticatedClient(
			orgUrl,
			sessionId,
			sessionCookieName
		);
		var oktaSession = await oktaAuthenticatedClient.GetCurrentOktaSessionAsync();
		if( oktaSession.Status != "ACTIVE" ) {
			if( BmxEnvironment.IsDebug ) {
				messageWriter.WriteWarning( "Okta passwordless authentication failed" );
			}
			return null;
		}

		string sessionLogin = oktaSession.Login.Split( "@" )[0];
		string providedLogin = user.Split( "@" )[0];
		if( !sessionLogin.Equals( providedLogin, StringComparison.OrdinalIgnoreCase ) ) {
			messageWriter.WriteWarning( $"""
				Okta passwordless authentication failed.
				The provided Okta user '{providedLogin}' does not match the system configured passwordless user '{sessionLogin}'.
				""" );
			return null;
		}

		TryCacheOktaSession(
			user,
			orgUrl.Host,
			sessionId,
			oktaSession.ExpiresAt,
			sessionCookieName
		);
		return oktaAuthenticatedClient;
	}

	private async Task<(string Name, string Value)?> GetSessionCookieFromBrowserAsync(
		string browserPath,
		Uri orgUrl,
		int timeoutSeconds
	) {
		if( BmxEnvironment.IsDebug ) {
			messageWriter.WriteWarning( $"Launching browser: {browserPath}" );
		}
		await using var browser = await browserLauncher.LaunchAsync( browserPath );

		var sessionCookieTcs = new TaskCompletionSource<(string Name, string Value)?>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);

		// cancel if the total time exceeds the configured timeout, including all page loads and retries
		using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( timeoutSeconds ) );
		cancellationTokenSource.Token.Register( () => sessionCookieTcs.TrySetCanceled() );

		// cancel if we can't load a page within half the total timeout
		using var pageTimer = new System.Timers.Timer(
			TimeSpan.FromSeconds( timeoutSeconds / 2.0 ) ) { AutoReset = false };
		pageTimer.Elapsed += ( _, _ ) => cancellationTokenSource.Cancel();
		pageTimer.Start();

		if( BmxEnvironment.IsDebug ) {
			messageWriter.WriteWarning( "Creating new browser tab" );
		}
		using var page = await browser.NewPageAsync().WaitAsync( cancellationTokenSource.Token );
		int attempt = 1;

		page.Load += ( _, _ ) => _ = OnPageLoadAsync();

		if( BmxEnvironment.IsDebug ) {
			messageWriter.WriteWarning( $"Navigating to {orgUrl}" );
		}
		await page.GoToAsync( orgUrl.AbsoluteUri ).WaitAsync( cancellationTokenSource.Token );
		return await sessionCookieTcs.Task;

		async Task OnPageLoadAsync() {
			// reset the per-page timer on every page load
			lock( pageTimer ) {
				pageTimer.Stop();
				pageTimer.Start();
			}

			string title = await page.GetTitleAsync().WaitAsync( cancellationTokenSource.Token );
			var url = new Uri( page.Url );
			if( BmxEnvironment.IsDebug ) {
				// Excludes any query parameters to prevent sensitive information from being logged
				messageWriter.WriteWarning(
					$"Browser loaded {url.GetLeftPart( UriPartial.Path )} with title '{title}'"
				);
			}

			if(
				url.Host == orgUrl.Host
				&& title.Contains( "sign in", StringComparison.OrdinalIgnoreCase )
			) {
				// DSSO can sometimes take more than one attempt. Both this terminal page and the
				// transient Agentless DSSO page have "Sign In" titles, so only retry from this path.
				if( url.AbsolutePath.Equals( "/login/agentlessDsso/idx", StringComparison.OrdinalIgnoreCase ) ) {
					if( attempt < 3 ) {
						if( BmxEnvironment.IsDebug ) {
							messageWriter.WriteWarning( $"Attempt {attempt} failed. Retry..." );
						}
						attempt++;
						await page.GoToAsync( orgUrl.AbsoluteUri ).WaitAsync( cancellationTokenSource.Token );
					} else {
						if( BmxEnvironment.IsDebug ) {
							messageWriter.WriteWarning( "Okta passwordless authentication is not available" );
						}
						sessionCookieTcs.SetResult( null );
					}
					return;
				}

				// Reaching this route AND the text being 'sign in' means we received a 200 and DSSO is not
				// going to work. The happy path also uses this route but returns a 302 so we wouldn't reach this
				if( url.AbsolutePath.Equals( "/oauth2/v1/authorize" ) ) {
					if( BmxEnvironment.IsDebug ) {
						messageWriter.WriteWarning( "Okta DSSO not supported in the current environment" );
					}
					sessionCookieTcs.SetResult( null );
				}
			}
			var cookies = await page.GetCookiesAsync( orgUrl.AbsoluteUri ).WaitAsync( cancellationTokenSource.Token );
			var sessionCookie = Array.Find(
				cookies,
				cookie => cookie.Name == OktaSessionCookieNames.IdentityEngine
					|| cookie.Name == OktaSessionCookieNames.Classic
			);
			if( sessionCookie is not null ) {
				sessionCookieTcs.SetResult( (sessionCookie.Name, sessionCookie.Value) );
			}
		}
	}

	private async Task<IOktaAuthenticatedClient> GetPasswordAuthenticatedClientAsync( Uri orgUrl, string user ) {
		string password = consolePrompter.PromptPassword();

		var oktaAnonymous = oktaClientFactory.CreateAnonymousClient( orgUrl );
		var authnResponse = await oktaAnonymous.AuthenticateAsync( user, password );

		if( authnResponse is AuthenticateResponse.Failure failure ) {
			throw new BmxException( $"""
				Okta authentication for user '{user}' in org '{orgUrl.Host}' failed ({failure.StatusCode}).
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
			TryCacheOktaSession(
				user,
				orgUrl.Host,
				sessionResp.Id,
				sessionResp.ExpiresAt,
				OktaSessionCookieNames.Classic
			);
			return oktaClientFactory.CreateAuthenticatedClient(
				orgUrl,
				sessionResp.Id,
				OktaSessionCookieNames.Classic
			);
		}

		if( authnResponse is AuthenticateResponse.Failure failure2 ) {
			throw new BmxException( $"Error verifying MFA with Okta ({failure2.StatusCode})." );
		}

		throw new UnreachableException( $"Unexpected response type: {authnResponse.GetType()}" );
	}

	private bool TryCacheOktaSession(
		string userId,
		string org,
		string sessionId,
		DateTimeOffset expiresAt,
		string sessionCookieName
	) {
		if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			CacheOktaSession( userId, org, sessionId, expiresAt, sessionCookieName );
			return true;
		}
		messageWriter.WriteWarning( """
			No config file found. Your Okta session will not be cached.
			Consider running `bmx configure` if you own this machine.
			""" );
		return false;
	}

	private void CacheOktaSession(
		string userId,
		string org,
		string sessionId,
		DateTimeOffset expiresAt,
		string sessionCookieName
	) {
		var session = new OktaSessionCache( userId, org, sessionId, expiresAt, sessionCookieName );
		var sessionsToCache = ReadOktaSessionCacheFile();
		sessionsToCache = sessionsToCache.Where( session => session.UserId != userId || session.Org != org )
			.ToList();
		sessionsToCache.Add( session );

		sessionStorage.SaveSessions( sessionsToCache );
	}

	private OktaSessionCache? GetCachedOktaSession( string userId, string org ) {
		if( !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			return null;
		}

		var oktaSessions = ReadOktaSessionCacheFile();
		return oktaSessions.Find( session => session.UserId == userId && session.Org == org );
	}

	private List<OktaSessionCache> ReadOktaSessionCacheFile() {
		var sourceCache = sessionStorage.GetSessions();
		var currTime = DateTimeOffset.Now;
		return sourceCache.Where( session => session.ExpiresAt > currTime ).ToList();
	}
}
