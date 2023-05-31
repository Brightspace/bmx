using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

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
	Task<OktaAccountState> GetAccountsAsync( string accountType );
	Task<string> GetAccountAsync( OktaAccountState state, string selectedAccount );
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
			throw new BmxException( "Error adding session: http client base address is not defined" );
		}
	}

	async Task<AuthenticateResponse> IOktaApi.AuthenticateAsync( string username, string password ) {
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
		throw new BmxException( "Error authenticating Okta" );
	}

	async Task IOktaApi.IssueMfaChallengeAsync( string stateToken, string factorId ) {
		var response = await _httpClient.PostAsJsonAsync(
			$"authn/factors/{factorId}/verify",
			new IssueMfaChallengeRequest( stateToken ),
			SourceGenerationContext.Default.IssueMfaChallengeRequest );

		if( !response.IsSuccessStatusCode ) {
			throw new BmxException( "Error sending MFA challenge" );
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
		throw new BmxException( "Error authenticating Okta challenge MFA" );
	}

	async Task<OktaSession> IOktaApi.CreateSessionAsync( string sessionToken ) {
		var resp = await _httpClient.PostAsJsonAsync(
			"sessions",
			new CreateSessionRequest( sessionToken ),
			SourceGenerationContext.Default.CreateSessionRequest );

		var session = await JsonSerializer.DeserializeAsync(
			await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaSession );

		return session ?? throw new BmxException( "Error creating Okta session" );
	}

	async Task<OktaAccountState> IOktaApi.GetAccountsAsync( string accountType ) {
		var resp = await _httpClient.GetAsync( "users/me/appLinks" );

		var accounts = await JsonSerializer.DeserializeAsync(
			await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaAppArray );

		if( accounts is not null ) {
			return new OktaAccountState( accounts, accountType );
		}
		throw new BmxException( "Error retrieving Okta accounts" );
	}

	async Task<string> IOktaApi.GetAccountAsync( OktaAccountState state, string selectedAccount ) {
		var account = Array.Find(
			state.OktaApps,
			app => string.Equals( app.Label, selectedAccount, StringComparison.OrdinalIgnoreCase ) );
		if( account is not null ) {
			var linkUri = new Uri( account.LinkUrl );

			var resp = await _httpClient.GetAsync( linkUri );
			return ExtractAwsSaml( await resp.Content.ReadAsStringAsync() );
		}

		throw new BmxException( "Account could not be found" );
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

	private static string ExtractAwsSaml( string htmlResponse ) {
		// HTML page is fairly malformed, grab just the <input> with the SAML data for further processing
		string inputRegexPattern = "<input name=\"SAMLResponse\" type=\"hidden\" value=\".*?\"/>";
		var inputRegex = new Regex( inputRegexPattern, RegexOptions.Compiled );

		// Access the SAML data from the parsed <input>
		var inputXml = new XmlDocument();
		inputXml.LoadXml( inputRegex.Match( htmlResponse ).Value );

		var samlData = inputXml.SelectSingleNode( "//@value" );

		if( samlData is not null ) {
			return samlData.InnerText;
		}
		throw new BmxException( "Error extracting SAML data" );
	}
}
