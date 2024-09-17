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
		bool experimental
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

		var oktaAnonymous = oktaClientFactory.CreateAnonymousClient( org );

		if( !ignoreCache && TryAuthenticateFromCache( org, user, oktaClientFactory, out var oktaAuthenticated ) ) {
			return new OktaAuthenticatedContext( Org: org, User: user, Client: oktaAuthenticated );
		}
		if( await TryAuthenticateWithDSSOAsync( org, user, oktaClientFactory, experimental ) is { } dssoclient ) {
			return new OktaAuthenticatedContext( Org: org, User: user, Client: dssoclient );
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
		var cancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );
		var sessionIdTaskProducer = new TaskCompletionSource<string?>( TaskCreationOptions.RunContinuationsAsynchronously );
		var userEmailTaskProducer = new TaskCompletionSource<string?>( TaskCreationOptions.RunContinuationsAsynchronously );
		string? sessionId;
		string? userEmail;

		try {
			var page = await browser.NewPageAsync().WaitAsync( cancellationTokenSource.Token );
			string baseAddress = $"https://{org}.okta.com/";
			int attempt = 1;

			page.Load += ( _, _ ) => _ = GetSessionCookieAsync( cancellationTokenSource.Token );
			page.Response += ( _, responseCreatedEventArgs ) => _ = GetOktaUserEmailAsync(
				responseCreatedEventArgs.Response
			);
			await page.GoToAsync( baseAddress, timeout: 10000 );
			sessionId = await sessionIdTaskProducer.Task.WaitAsync( cancellationTokenSource.Token );
			userEmail = await userEmailTaskProducer.Task.WaitAsync( cancellationTokenSource.Token );

			async Task GetSessionCookieAsync( CancellationToken cancellationToken ) {
				var url = new Uri( page.Url );
				if( url.Host == $"{org}.okta.com" ) {
					string title = await page.GetTitleAsync();
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

			async Task GetOktaUserEmailAsync(
				IResponse response
			) {
				if( response.Url.Contains( $"{baseAddress}enduser/api/v1/home" ) ) {
					string content = await response.TextAsync();
					var home = JsonSerializer.Deserialize( content, JsonCamelCaseContext.Default.OktaHomeResponse );
					if( home is not null ) {
						userEmailTaskProducer.SetResult( home.Login );
					}
				}
			}

		} catch( TaskCanceledException ) {
			consoleWriter.WriteWarning(
				$"WARNING: Failed to create {org} Okta session through DSSO. Check if org is correct."
			);
			return null;
		} catch( TargetClosedException ) {
			consoleWriter.WriteWarning(
				"WARNING: Failed to create Okta session through DSSO. If running BMX with admin privileges, rerun the command with the '--experimental' flag."
			);
			return null;
		} catch( Exception e ) {
			consoleWriter.WriteWarning( "Error while trying to authenticate with Okta using DSSO." );
			consoleWriter.WriteError( e.GetType().ToString() );
			consoleWriter.WriteError( e.Message );
			return null;
		} finally {
			cancellationTokenSource.Dispose();
			browser.Dispose();
		}

		if( sessionId is null || userEmail is null ) {
			return null;
		} else if( !OktaUserMatchesProvided( userEmail, user ) ) {
			consoleWriter.WriteWarning(
				"WARNING: Could not create Okta session using DSSO as "
				+ $"provided Okta user '{user}' does not match user '{userEmail}'." );
			return null;
		}

		var oktaAuthenticatedClient = oktaClientFactory.CreateAuthenticatedClient( org, sessionId );
		var sessionExpiry = await oktaAuthenticatedClient.GetSessionExpiryAsync();
		CacheOktaSession( user, org, sessionId, sessionExpiry );
		return oktaAuthenticatedClient;
	}

	private static bool OktaUserMatchesProvided( string oktaLogin, string providedUser ) {
		string adName = oktaLogin.Split( '@' )[0];
		string normalizedUser = providedUser.Contains( '@' )
			? providedUser.Split( '@' )[0]
			: providedUser;
		return adName.Equals( normalizedUser, StringComparison.OrdinalIgnoreCase );
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
