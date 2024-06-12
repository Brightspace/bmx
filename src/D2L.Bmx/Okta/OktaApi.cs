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
}

internal class OktaApi : IOktaApi {
	private readonly CookieContainer _cookieContainer;
	private readonly HttpClient _httpClient;
	private string? _organization;

	public OktaApi() {
		_cookieContainer = new CookieContainer();
		_httpClient = new HttpClient( new HttpClientHandler { CookieContainer = _cookieContainer } ) {
			Timeout = TimeSpan.FromSeconds( 30 )
		};
		_httpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
	}

	void IOktaApi.SetOrganization( string organization ) {
		_organization = organization;
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
		HttpResponseMessage resp;
		try {
			resp = await _httpClient.PostAsJsonAsync(
				"authn",
				new AuthenticateRequest( username, password ),
				JsonCamelCaseContext.Default.AuthenticateRequest );
		} catch( Exception ex ) {
			throw new BmxException( "Okta authentication request failed.", ex );
		}

		AuthenticateResponseRaw? authnResponse;
		try {
			authnResponse = await JsonSerializer.DeserializeAsync(
				await resp.Content.ReadAsStreamAsync(),
				JsonCamelCaseContext.Default.AuthenticateResponseRaw );
		} catch( Exception ex ) {
			throw new BmxException( "Okta authentication failed. Okta returned an invalid response", ex );
		}

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

		string org = _organization is not null ? _organization : "unknown";
		throw new BmxException(
			$"Okta authentication for user '{username}' in org '{org}'"
				+ "failed. Check if org, user, and password is correct" );
	}

	async Task IOktaApi.IssueMfaChallengeAsync( string stateToken, string factorId ) {
		try {
			var response = await _httpClient.PostAsJsonAsync(
			$"authn/factors/{factorId}/verify",
			new IssueMfaChallengeRequest( stateToken ),
			JsonCamelCaseContext.Default.IssueMfaChallengeRequest );

			response.EnsureSuccessStatusCode();
		} catch( Exception ex ) {
			throw new BmxException( "Error starting MFA with Okta", ex );
		}
	}

	async Task<AuthenticateResponse> IOktaApi.VerifyMfaChallengeResponseAsync(
		string stateToken,
		string factorId,
		string challengeResponse
	) {
		var request = new VerifyMfaChallengeResponseRequest(
			StateToken: stateToken,
			PassCode: challengeResponse );
		HttpResponseMessage resp;
		try {
			resp = await _httpClient.PostAsJsonAsync(
				$"authn/factors/{factorId}/verify",
				request,
				JsonCamelCaseContext.Default.VerifyMfaChallengeResponseRequest );
		} catch( Exception ex ) {
			throw new BmxException( "Okta MFA verification request failed.", ex );
		}

		AuthenticateResponseRaw? authnResponse;
		try {
			authnResponse = await JsonSerializer.DeserializeAsync(
					await resp.Content.ReadAsStreamAsync(),
					JsonCamelCaseContext.Default.AuthenticateResponseRaw );
		} catch( Exception ex ) {
			throw new BmxException( "Error verifying MFA with Okta. Okta returned an invalid response", ex );
		}

		if( authnResponse?.SessionToken is not null ) {
			return new AuthenticateResponse.Success( authnResponse.SessionToken );
		}
		throw new BmxException( "Error verifying MFA with Okta." );
	}

	async Task<OktaSession> IOktaApi.CreateSessionAsync( string sessionToken ) {
		HttpResponseMessage resp;
		try {
			resp = await _httpClient.PostAsJsonAsync(
				"sessions",
				new CreateSessionRequest( sessionToken ),
				JsonCamelCaseContext.Default.CreateSessionRequest );
			resp.EnsureSuccessStatusCode();
		} catch( Exception ex ) {
			throw new BmxException( "Request to create Okta Session failed.", ex );
		}

		OktaSession? session;
		try {
			session = await JsonSerializer.DeserializeAsync(
			await resp.Content.ReadAsStreamAsync(),
			JsonCamelCaseContext.Default.OktaSession );
		} catch( Exception ex ) {
			throw new BmxException( "Error creating Okta Session. Okta returned an invalid response", ex );
		}

		return session ?? throw new BmxException( "Error creating Okta Session." );
	}

	async Task<OktaApp[]> IOktaApi.GetAwsAccountAppsAsync() {
		OktaApp[]? apps;
		try {
			apps = await _httpClient.GetFromJsonAsync(
				"users/me/appLinks",
				JsonCamelCaseContext.Default.OktaAppArray );
		} catch( Exception ex ) {
			throw new BmxException( "Request to retrieve AWS accounts from Okta failed.", ex );
		}

		return apps?.Where( app => app.AppName == "amazon_aws" ).ToArray()
				?? throw new BmxException( "Error retrieving AWS accounts from Okta." );
	}

	async Task<string> IOktaApi.GetPageAsync( string samlLoginUrl ) {
		return await _httpClient.GetStringAsync( samlLoginUrl );
	}
}
