using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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

		string orgBaseAddress = GetOrgBaseAddress( org );
		var oktaAnonymous = oktaClientFactory.CreateAnonymousClient( orgBaseAddress );

		if( !ignoreCache && TryAuthenticateFromCache( org, user, oktaClientFactory, out var oktaAuthenticated ) ) {
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaAuthenticated );
		}
		if( await GetDssoAuthenticatedClientAsync(
			orgBaseAddress,
			org,
			user,
			oktaClientFactory,
			nonInteractive,
			bypassBrowserSecurity ) is { } oktaDssoAuthenticated
		) {
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaDssoAuthenticated );
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
			TryCacheOktaSession( user, org, sessionResp.Id, sessionResp.ExpiresAt );
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaAuthenticated );
		}

		if( authnResponse is AuthenticateResponse.Failure failure2 ) {
			throw new BmxException( $"Error verifying MFA with Okta ({failure2.StatusCode})." );
		}

		throw new UnreachableException( $"Unexpected response type: {authnResponse.GetType()}" );
	}

	private static string GetOrgBaseAddress( string org ) {
		return org.Contains( '.' )
			? $"https://{org}/"
			: $"https://{org}.okta.com/";
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

	private async Task<IOktaAuthenticatedClient?> GetDssoAuthenticatedClientAsync(
		string orgBaseAddress,
		string org,
		string user,
		IOktaClientFactory oktaClientFactory,
		bool nonInteractive,
		bool experimentalBypassBrowserSecurity
	) {
		await using IBrowser? browser = await Browser.LaunchBrowserAsync( experimentalBypassBrowserSecurity );
		if( browser is null ) {
			return null;
		}

		Console.WriteLine( DateTimeOffset.Now );
		if( !nonInteractive ) {
			Console.Error.WriteLine( "Attempting to automatically login using Okta Desktop Single Sign-On." );
		}
		using var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 15 ) );
		var sessionIdTcs = new TaskCompletionSource<string?>( TaskCreationOptions.RunContinuationsAsynchronously );
		var userEmailTcs = new TaskCompletionSource<string?>( TaskCreationOptions.RunContinuationsAsynchronously );
		string? sessionId;
		string? userEmail;

		try {
			using var page = await browser.NewPageAsync().WaitAsync( cancellationTokenSource.Token );
			var baseUrl = new Uri( orgBaseAddress );
			int attempt = 1;

			page.Load += ( _, _ ) => _ = GetSessionCookieAsync().WaitAsync( cancellationTokenSource.Token );
			page.Response += ( _, response ) => _ = GetOktaUserEmailAsync( response.Response ).WaitAsync(
				cancellationTokenSource.Token );
			await page.GoToAsync( orgBaseAddress ).WaitAsync( cancellationTokenSource.Token );
			sessionId = await sessionIdTcs.Task;
			userEmail = await userEmailTcs.Task;

			async Task GetSessionCookieAsync() {
				var url = new Uri( page.Url );
				if( url.Host == baseUrl.Host ) {
					string title = await page.GetTitleAsync();
					// DSSO can sometimes takes more than one attempt.
					// If the path is '/' with 'sign in' in the title, it means DSSO is not available and we should stop retrying.
					if( title.Contains( "sign in", StringComparison.OrdinalIgnoreCase ) ) {
						if( attempt < 3 && url.AbsolutePath != "/" ) {
							attempt++;
							await page.GoToAsync( orgBaseAddress );
						} else {
							consoleWriter.WriteWarning(
								"WARNING: Could not authenticate with Okta using Desktop Single Sign-On." );
							sessionIdTcs.SetResult( null );
						}
						return;
					}
				}
				var cookies = await page.GetCookiesAsync( orgBaseAddress );
				if( Array.Find( cookies, c => c.Name == "sid" )?.Value is string sid ) {
					sessionIdTcs.SetResult( sid );
				}
			}
			async Task GetOktaUserEmailAsync( IResponse response ) {
				if( response.Url.Contains( $"{orgBaseAddress}enduser/api/v1/home" ) ) {
					string content = await response.TextAsync();
					var home = JsonSerializer.Deserialize( content, JsonCamelCaseContext.Default.OktaHomeResponse );
					if( home is not null ) {
						userEmailTcs.SetResult( home.Login );
					}
				}
			}
		} catch( TaskCanceledException ) {
			consoleWriter.WriteWarning( $"""
				WARNING: Timed out when trying to create Okta session through Desktop Single Sign-On.
				Check if the org '{orgBaseAddress}' is correct. If running BMX with elevated privileges,
				rerun the command with the '--experimental-bypass-browser-security' flag
				"""
			);
			return null;
		} catch( TargetClosedException ) {
			consoleWriter.WriteWarning( """
				WARNING: Failed to create Okta session through Desktop Single Sign-On as BMX is likely being run
				with elevated privileges. Rerun the command with the '--experimental-bypass-browser-security' flag.
				"""
			);
			return null;
		} catch( Exception ) {
			consoleWriter.WriteWarning(
				"WARNING: Unknown error while trying to authenticate with Okta using Desktop Single Sign-On." );
			return null;
		}

		if( sessionId is null || userEmail is null ) {
			return null;
		} else if( !OktaUserMatchesProvided( userEmail, user ) ) {
			consoleWriter.WriteWarning(
				"WARNING: Could not create Okta session using Desktop Single Sign-On as "
				+ $"provided Okta user '{StripLoginDomain( user )}' does not match user '{StripLoginDomain( userEmail )}'." );
			return null;
		}

		var oktaAuthenticatedClient = oktaClientFactory.CreateAuthenticatedClient( orgBaseAddress, sessionId );
		var expiresAt = ( await oktaAuthenticatedClient.GetCurrentOktaSessionAsync() ).ExpiresAt;
		TryCacheOktaSession( user, org, sessionId, expiresAt );
		return oktaAuthenticatedClient;
	}

	private static string StripLoginDomain( string email ) {
		return email.Contains( '@' ) ? email.Split( '@' )[0] : email;
	}

	private static bool OktaUserMatchesProvided( string oktaLogin, string providedUser ) {
		string adName = StripLoginDomain( oktaLogin );
		string normalizedUser = StripLoginDomain( providedUser );
		return adName.Equals( normalizedUser, StringComparison.OrdinalIgnoreCase );
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
