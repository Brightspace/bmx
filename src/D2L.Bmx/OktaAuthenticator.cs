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
	IConsolePrompter consolePrompter,
	IConsoleWriter consoleWriter,
	BmxConfig config
) {
	public async Task<OktaAuthenticatedContext> AuthenticateAsync(
		string? org,
		string? user,
		bool nonInteractive,
		bool ignoreCache,
		bool bypassBrowserSecurity
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

		var orgUrl = GetOrgBaseAddress( org );

		if( !ignoreCache && TryAuthenticateFromCache( orgUrl, user, out var oktaAuthenticated ) ) {
			return new( Org: org, User: user, Client: oktaAuthenticated );
		}

		if( TryGetDssoAuthOptions( nonInteractive, bypassBrowserSecurity, out var dssoAuthOptions ) ) {
			if( !nonInteractive ) {
				Console.Error.WriteLine( "Attempting Okta passwordless authentication..." );
			}
			oktaAuthenticated = await GetDssoAuthenticatedClientAsync(
				orgUrl,
				user,
				dssoAuthOptions
			);
			if( oktaAuthenticated is not null ) {
				return new( Org: org, User: user, Client: oktaAuthenticated );
			}
			if( !nonInteractive ) {
				Console.Error.WriteLine( "Falling back to Okta password authentication..." );
			}
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
		string? sessionId = GetCachedOktaSessionId( user, orgBaseAddress.Host );
		if( string.IsNullOrEmpty( sessionId ) ) {
			oktaAuthenticated = null;
			return false;
		}

		oktaAuthenticated = oktaClientFactory.CreateAuthenticatedClient( orgBaseAddress, sessionId );
		return true;
	}

	private record DssoAuthOptions( string BrowserPath, bool BypassBrowserSecurity, bool PrintDebugMessage );

	private bool TryGetDssoAuthOptions(
		bool nonInteractive,
		bool bypassBrowserSecurity,
		[NotNullWhen( returnValue: true )] out DssoAuthOptions? options
	) {
		options = null;

		bool printDebugMessage =
			BmxEnvironment.IsDebug
			|| (
				// if the user passed --experimental-bypass-browser-security, they've explicitly expressed interest in the browser/DSSO process
				bypassBrowserSecurity
				// but we still shouldn't print messages in non-interactive mode (it may break credential process)
				&& !nonInteractive
			);

		bool hasElevatedPermissions = UserPrivileges.HasElevatedPermissions();
		if( hasElevatedPermissions && !bypassBrowserSecurity ) {
			if( printDebugMessage ) {
				consoleWriter.WriteWarning(
					"BMX is being run with elevated privileges and is unable to use Okta passwordless authentication."
				);
			}
			return false;
		}
		if( !hasElevatedPermissions && bypassBrowserSecurity ) {
			// We want to avoid providing '--no-sandbox' to chromium unless absolutely necessary.
			bypassBrowserSecurity = false;
			if( printDebugMessage ) {
				consoleWriter.WriteWarning(
					"Ignoring '--experimental-bypass-browser-security', "
					+ "as it's only needed when BMX is run with elevated privileges."
				);
			}
		}

		if( !Browser.TryGetPathToBrowser( out string? browserPath ) ) {
			if( printDebugMessage ) {
				consoleWriter.WriteWarning(
					"No suitable browser found for Okta passwordless authentication"
				);
			}
			return false;
		}

		options = new( browserPath, bypassBrowserSecurity, printDebugMessage );
		return true;
	}

	private async Task<IOktaAuthenticatedClient?> GetDssoAuthenticatedClientAsync(
		Uri orgUrl,
		string user,
		DssoAuthOptions options
	) {
		await using var browser = await Browser.LaunchBrowserAsync( options.BrowserPath, options.BypassBrowserSecurity );

		using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 15 ) );
		var sessionIdTcs = new TaskCompletionSource<string?>( TaskCreationOptions.RunContinuationsAsynchronously );
		string? sessionId = null;

		try {
			using var page = await browser.NewPageAsync().WaitAsync( cancellationTokenSource.Token );
			int attempt = 1;

			page.Load += ( _, _ ) => _ = GetSessionCookieAsync();
			await page.GoToAsync( orgUrl.AbsoluteUri ).WaitAsync( cancellationTokenSource.Token );
			sessionId = await sessionIdTcs.Task;

			async Task GetSessionCookieAsync() {
				var url = new Uri( page.Url );
				if( url.Host == orgUrl.Host ) {
					string title = await page.GetTitleAsync().WaitAsync( cancellationTokenSource.Token );
					// DSSO can sometimes takes more than one attempt.
					// If the path is '/' with 'sign in' in the title, it means DSSO is not available and we should stop retrying.
					if( title.Contains( "sign in", StringComparison.OrdinalIgnoreCase ) ) {
						if( attempt < 3 && url.AbsolutePath != "/" ) {
							attempt++;
							await page.GoToAsync( orgUrl.AbsoluteUri ).WaitAsync( cancellationTokenSource.Token );
						} else {
							if( options.PrintDebugMessage ) {
								if( url.AbsolutePath == "/" ) {
									consoleWriter.WriteWarning(
										"Okta passwordless authentication is not available."
									);
								} else {
									consoleWriter.WriteWarning( "Okta passwordless authentication failed" );
								}
							}
							sessionIdTcs.SetResult( null );
						}
						return;
					}
				}
				var cookies = await page.GetCookiesAsync( orgUrl.AbsoluteUri ).WaitAsync( cancellationTokenSource.Token );
				if( Array.Find( cookies, c => c.Name == "sid" )?.Value is string sid ) {
					sessionIdTcs.SetResult( sid );
				}
			}
		} catch( TaskCanceledException ) {
			if( options.PrintDebugMessage ) {
				consoleWriter.WriteWarning( "Okta passwordless authentication timed out." );
			}
		} catch( Exception ) {
			if( options.PrintDebugMessage ) {
				consoleWriter.WriteWarning( "Unknown error occurred while trying Okta passwordless authentication." );
			}
		}

		if( sessionId is null ) {
			return null;
		}

		var oktaAuthenticatedClient = oktaClientFactory.CreateAuthenticatedClient( orgUrl, sessionId );
		var oktaSession = await oktaAuthenticatedClient.GetCurrentOktaSessionAsync();
		string sessionLogin = oktaSession.Login.Split( "@" )[0];
		string providedLogin = user.Split( "@" )[0];
		if( !sessionLogin.Equals( providedLogin, StringComparison.OrdinalIgnoreCase ) ) {
			consoleWriter.WriteWarning( $"""
				Okta passwordless authentication failed.
				The provided Okta user '{providedLogin}' does not match the system configured passwordless user '{sessionLogin}'.
				""" );
			return null;
		}

		TryCacheOktaSession( user, orgUrl.Host, sessionId, oktaSession.ExpiresAt );
		return oktaAuthenticatedClient;
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
			TryCacheOktaSession( user, orgUrl.Host, sessionResp.Id, sessionResp.ExpiresAt );
			return oktaClientFactory.CreateAuthenticatedClient( orgUrl, sessionResp.Id );
		}

		if( authnResponse is AuthenticateResponse.Failure failure2 ) {
			throw new BmxException( $"Error verifying MFA with Okta ({failure2.StatusCode})." );
		}

		throw new UnreachableException( $"Unexpected response type: {authnResponse.GetType()}" );
	}

	private bool TryCacheOktaSession( string userId, string org, string sessionId, DateTimeOffset expiresAt ) {
		if( File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			CacheOktaSession( userId, org, sessionId, expiresAt );
			return true;
		}
		consoleWriter.WriteWarning( """
			No config file found. Your Okta session will not be cached.
			Consider running `bmx configure` if you own this machine.
			""" );
		return false;
	}

	private void CacheOktaSession( string userId, string org, string sessionId, DateTimeOffset expiresAt ) {
		var session = new OktaSessionCache( userId, org, sessionId, expiresAt );
		var sessionsToCache = ReadOktaSessionCacheFile();
		sessionsToCache = sessionsToCache.Where( session => session.UserId != userId || session.Org != org )
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
