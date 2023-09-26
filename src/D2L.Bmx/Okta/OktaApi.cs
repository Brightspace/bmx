using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal interface IOktaApi {
	void SetOrganization( string organization );
	void AddSession( string sessionId );
	Task<AuthenticateResponse> AuthenticateAsync( string username, string password );
	Task IssueMfaChallengeAsync( string stateToken, string factorId );
	Task<AuthenticateResponse> VerifyMfaChallengeResponseAsync(
		string stateToken,
		string factorId,
		string challengeResponse );
	Task<OktaSession> CreateSessionAsync( string sessionToken );
	Task<OktaApp[]> GetAwsAccountAppsAsync();
	Task<string> GetPageAsync( string samlLoginUrl );
	Task<string?> GetCurrentUserIdAsync( string sessionId );
}

internal class OktaApi : IOktaApi {
	private readonly CookieContainer _cookieContainer;
	private readonly HttpClient _httpClient;

	public OktaApi() {
		_cookieContainer = new CookieContainer();
		_httpClient = new HttpClient( new HttpClientHandler { CookieContainer = _cookieContainer } );
		_httpClient.Timeout = TimeSpan.FromSeconds( 30 );
		_httpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
	}

	void IOktaApi.SetOrganization( string organization ) {
		if( !organization.Contains( '.' ) ) {
			_httpClient.BaseAddress = new Uri( $"https://{organization}.okta.com/api/v1/" );
		} else {
			_httpClient.BaseAddress = new Uri( $"https://{organization}/api/v1/" );
		}
	}

	void IOktaApi.AddSession( string sessionId ) {
		if( _httpClient.BaseAddress is not null ) {
			_cookieContainer.Add( new Cookie( "sid", sessionId, "/", _httpClient.BaseAddress.Host ) );
		} else {
			throw new InvalidOperationException( "Error adding session: http client base address is not defined" );
		}
	}

	async Task<AuthenticateResponse> IOktaApi.AuthenticateAsync( string username, string password ) {
		try {
			var resp = await _httpClient.PostAsJsonAsync(
				"authn",
				new AuthenticateRequest( username, password ),
				SourceGenerationContext.Default.AuthenticateRequest );
			var authnResponse = await JsonSerializer.DeserializeAsync(
				await resp.Content.ReadAsStreamAsync(),
				SourceGenerationContext.Default.AuthenticateResponseRaw );

			if( authnResponse is {
				SessionToken: not null,
				Status: AuthenticationStatus.SUCCESS
			} ) {
				return new AuthenticateResponse.Success( authnResponse.SessionToken );
			}
			if( authnResponse is {
				StateToken: not null,
				Embedded.Factors: not null,
				Status: AuthenticationStatus.MFA_REQUIRED
			} ) {
				return new AuthenticateResponse.MfaRequired(
					authnResponse.StateToken,
					authnResponse.Embedded.Factors
				);
			}
			throw new BmxException( "Okta API Authentication Response was not Success or MFA Required" );
		} catch( Exception ex ) {
			if( ex is BmxException ) {
				throw new BmxException( "Okta authentication failed. Check if org, user and password is correct", ex );
			}
			throw new BmxException( "Error authenticating to Okta", ex );
		}
	}

	async Task IOktaApi.IssueMfaChallengeAsync( string stateToken, string factorId ) {
		try {
			var response = await _httpClient.PostAsJsonAsync(
			$"authn/factors/{factorId}/verify",
			new IssueMfaChallengeRequest( stateToken ),
			SourceGenerationContext.Default.IssueMfaChallengeRequest );

			response.EnsureSuccessStatusCode();
		} catch( Exception ex ) {
			throw new BmxException( "Error sending MFA challenge", ex );
		}
	}

	async Task<AuthenticateResponse> IOktaApi.VerifyMfaChallengeResponseAsync(
		string stateToken,
		string factorId,
		string challengeResponse
	) {
		try {
			var request = new VerifyMfaChallengeResponseRequest(
				StateToken: stateToken,
				PassCode: challengeResponse );

			var resp = await _httpClient.PostAsJsonAsync(
				$"authn/factors/{factorId}/verify",
				request,
				SourceGenerationContext.Default.VerifyMfaChallengeResponseRequest );

			var authnResponse = await JsonSerializer.DeserializeAsync(
				await resp.Content.ReadAsStreamAsync(),
				SourceGenerationContext.Default.AuthenticateResponseRaw );
			if( authnResponse?.SessionToken is not null ) {
				return new AuthenticateResponse.Success( authnResponse.SessionToken );
			}
			throw new BmxException( "Okta MFA challenge response session token null" );
		} catch( Exception ex ) {
			throw new BmxException( "Error verifying Okta MFA challenge response", ex );
		}
	}

	async Task<OktaSession> IOktaApi.CreateSessionAsync( string sessionToken ) {
		try {
			var resp = await _httpClient.PostAsJsonAsync(
				"sessions",
				new CreateSessionRequest( sessionToken ),
				SourceGenerationContext.Default.CreateSessionRequest );

			var session = await JsonSerializer.DeserializeAsync(
				await resp.Content.ReadAsStreamAsync(),
				SourceGenerationContext.Default.OktaSession );
			return session ?? throw new BmxException( "Creating Okta Session returned null" );
		} catch( Exception ex ) {
			throw new BmxException( "Error creating Okta session", ex );
		}
	}

	async Task<OktaApp[]> IOktaApi.GetAwsAccountAppsAsync() {
		try {
			var apps = await _httpClient.GetFromJsonAsync(
				"users/me/appLinks",
				SourceGenerationContext.Default.OktaAppArray );

			return apps?.Where( app => app.AppName == "amazon_aws" ).ToArray()
				?? throw new BmxException( "Retrieving AWS accounts from Okta returned null" );
		} catch( Exception ex ) {
			throw new BmxException( "Error retrieving AWS accounts from Okta", ex );
		}
	}

	async Task<string> IOktaApi.GetPageAsync( string samlLoginUrl ) {
		return await _httpClient.GetStringAsync( samlLoginUrl );
	}

	async Task<string?> IOktaApi.GetCurrentUserIdAsync( string sessionId ) {
		try {
			using var meResponse = await _httpClient.GetAsync( "users/me" );
			if( !meResponse.IsSuccessStatusCode ) {
				return null;
			}
			var me = await meResponse.Content.ReadFromJsonAsync( SourceGenerationContext.Default.OktaMeResponse );
			return me?.Id;
		} catch( HttpRequestException ) {
			return null;
		} catch( JsonException ) {
			return null;
		}
	}
}
