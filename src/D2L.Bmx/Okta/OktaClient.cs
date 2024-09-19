using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx.Okta;

internal interface IOktaClientFactory {
	IOktaAnonymousClient CreateAnonymousClient( Uri orgUrl );
	IOktaAuthenticatedClient CreateAuthenticatedClient( Uri orgUrl, string sessionId );
}

internal interface IOktaAnonymousClient {
	Task<AuthenticateResponse> AuthenticateAsync( string username, string password );
	Task IssueMfaChallengeAsync( string stateToken, string factorId );
	Task<AuthenticateResponse> VerifyMfaChallengeResponseAsync(
		string stateToken,
		string factorId,
		string challengeResponse
	);
	Task<OktaSession> CreateSessionAsync( string sessionToken );
}

internal interface IOktaAuthenticatedClient {
	Task<OktaApp[]> GetAwsAccountAppsAsync();
	Task<OktaSession> GetCurrentOktaSessionAsync();
	Task<string> GetPageAsync( string url );
}

internal class OktaClientFactory : IOktaClientFactory {
	IOktaAnonymousClient IOktaClientFactory.CreateAnonymousClient( Uri orgUrl ) {
		var httpClient = new HttpClient {
			Timeout = TimeSpan.FromSeconds( 30 ),
			BaseAddress = GetApiBaseAddress( orgUrl ),
		};
		return new OktaAnonymousClient( httpClient );
	}

	IOktaAuthenticatedClient IOktaClientFactory.CreateAuthenticatedClient( Uri orgUrl, string sessionId ) {
		var baseAddress = GetApiBaseAddress( orgUrl );

		var cookieContainer = new CookieContainer();
		cookieContainer.Add( new Cookie( "sid", sessionId, "/", baseAddress.Host ) );

		var httpClient = new HttpClient( new SocketsHttpHandler {
			CookieContainer = cookieContainer,
		} ) {
			Timeout = TimeSpan.FromSeconds( 30 ),
			BaseAddress = baseAddress,
		};

		return new OktaAuthenticatedClient( httpClient );
	}

	private static Uri GetApiBaseAddress( Uri orgBaseAddresss ) {
		return new Uri( orgBaseAddresss, "api/v1/" );
	}
}

internal class OktaAnonymousClient( HttpClient httpClient ) : IOktaAnonymousClient {
	async Task<AuthenticateResponse> IOktaAnonymousClient.AuthenticateAsync( string username, string password ) {
		HttpResponseMessage resp;
		try {
			resp = await httpClient.PostAsJsonAsync(
				"authn",
				new AuthenticateRequest( username, password ),
				JsonCamelCaseContext.Default.AuthenticateRequest
			);
		} catch( Exception ex ) {
			throw new BmxException( "Okta authentication request failed.", ex );
		}

		AuthenticateResponseRaw? authnResponse;
		try {
			authnResponse = await JsonSerializer.DeserializeAsync(
				await resp.Content.ReadAsStreamAsync(),
				JsonCamelCaseContext.Default.AuthenticateResponseRaw
			);
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

		return new AuthenticateResponse.Failure( resp.StatusCode );
	}

	async Task IOktaAnonymousClient.IssueMfaChallengeAsync( string stateToken, string factorId ) {
		try {
			var response = await httpClient.PostAsJsonAsync(
			$"authn/factors/{factorId}/verify",
			new IssueMfaChallengeRequest( stateToken ),
			JsonCamelCaseContext.Default.IssueMfaChallengeRequest );

			response.EnsureSuccessStatusCode();
		} catch( Exception ex ) {
			throw new BmxException( "Error starting MFA with Okta", ex );
		}
	}

	async Task<AuthenticateResponse> IOktaAnonymousClient.VerifyMfaChallengeResponseAsync(
		string stateToken,
		string factorId,
		string challengeResponse
	) {
		var request = new VerifyMfaChallengeResponseRequest(
			StateToken: stateToken,
			PassCode: challengeResponse );
		HttpResponseMessage resp;
		try {
			resp = await httpClient.PostAsJsonAsync(
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
		return new AuthenticateResponse.Failure( resp.StatusCode );
	}

	async Task<OktaSession> IOktaAnonymousClient.CreateSessionAsync( string sessionToken ) {
		HttpResponseMessage resp;
		try {
			resp = await httpClient.PostAsJsonAsync(
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
}

internal class OktaAuthenticatedClient( HttpClient httpClient ) : IOktaAuthenticatedClient {
	async Task<OktaApp[]> IOktaAuthenticatedClient.GetAwsAccountAppsAsync() {
		OktaApp[]? apps;
		try {
			apps = await httpClient.GetFromJsonAsync(
				"users/me/appLinks",
				JsonCamelCaseContext.Default.OktaAppArray );
		} catch( Exception ex ) {
			throw new BmxException( "Request to retrieve AWS accounts from Okta failed.", ex );
		}

		return apps?.Where( app => app.AppName == "amazon_aws" ).ToArray()
				?? throw new BmxException( "Error retrieving AWS accounts from Okta." );
	}

	async Task<OktaSession> IOktaAuthenticatedClient.GetCurrentOktaSessionAsync() {
		OktaSession? session;
		try {
			session = await httpClient.GetFromJsonAsync(
				"sessions/me",
				JsonCamelCaseContext.Default.OktaSession );
		} catch( Exception ex ) {
			throw new BmxException( "Request to retrieve session from Okta failed.", ex );
		}

		return session ?? throw new BmxException( "Error retrieving session from Okta." );
	}

	async Task<string> IOktaAuthenticatedClient.GetPageAsync( string url ) {
		return await httpClient.GetStringAsync( url );
	}
}
