using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

namespace D2L.Bmx.Okta;

internal interface IOktaApi {
	void SetOrganization( string organization );
	void AddSession( string sessionId );
	Task<OktaAuthenticateState> AuthenticateAsync( AuthenticateOptions authOptions );
	Task<string> AuthenticateChallengeMfaAsync( OktaAuthenticateState state, int selectedMfaIndex,
		string challengeResponse );
	Task IssueMfaChallengeAsync( OktaAuthenticateState state, int selectedMfaIndex );
	Task<OktaSession> CreateSessionAsync( SessionOptions sessionOptions );
	Task<OktaAccountState> GetAccountsAsync( string accountType );
	Task<string> GetAccountAsync( OktaAccountState state, string selectedAccount );
	Task<string?> GetMeResponseAsync( string sessionId );
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
		_httpClient.BaseAddress = new Uri( $"https://{organization}.okta.com/api/v1/" );
	}

	void IOktaApi.AddSession( string sessionId ) {
		if( _httpClient.BaseAddress is not null ) {
			_cookieContainer.Add( new Cookie( "sid", sessionId, "/", _httpClient.BaseAddress.Host ) );
		} else {
			throw new BmxException( "Error adding session: http client base address is not defined" );
		}
	}

	async Task<OktaAuthenticateState> IOktaApi.AuthenticateAsync( AuthenticateOptions authOptions ) {

		var resp = await _httpClient.PostAsync( "authn",
			new StringContent(
				JsonSerializer.Serialize( authOptions, SourceGenerationContext.Default.AuthenticateOptions ),
				Encoding.Default,
				"application/json" ) );
		var authInitial = await JsonSerializer.DeserializeAsync(
			await resp.Content.ReadAsStreamAsync(), SourceGenerationContext.Default.AuthenticateResponseInital );

		if( authInitial?.StateToken is not null || authInitial?.SessionToken is not null ) {
			return new OktaAuthenticateState(
				OktaStateToken: authInitial.StateToken,
				OktaSessionToken: authInitial.SessionToken,
				authInitial.Embedded.Factors );
		}
		throw new BmxException( "Error authenticating Okta" );
	}

	async Task IOktaApi.IssueMfaChallengeAsync( OktaAuthenticateState state, int selectedMfaIndex ) {
		var mfaFactor = state.OktaMfaFactors[selectedMfaIndex];
		var authOptions = new AuthenticateChallengeMfaOptions( FactorId: mfaFactor.Id, StateToken: state.OktaStateToken! );

		await _httpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
			new StringContent(
				JsonSerializer.Serialize( authOptions, SourceGenerationContext.Default.AuthenticateChallengeMfaOptions ),
				Encoding.Default,
				"application/json" ) );
	}

	async Task<string> IOktaApi.AuthenticateChallengeMfaAsync(
		OktaAuthenticateState state, int selectedMfaIndex,
		string challengeResponse ) {

		var mfaFactor = state.OktaMfaFactors[selectedMfaIndex];

		var authOptions = new AuthenticateChallengeMfaOptions(
			FactorId: mfaFactor.Id,
			StateToken: state.OktaStateToken!,
			PassCode: challengeResponse );

		var resp = await _httpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
			new StringContent(
				JsonSerializer.Serialize( authOptions, SourceGenerationContext.Default.AuthenticateChallengeMfaOptions ),
				Encoding.Default,
				"application/json" ) );
		var authSuccess = await JsonSerializer.DeserializeAsync<AuthenticateResponseSuccess>(
			await resp.Content.ReadAsStreamAsync(), SourceGenerationContext.Default.AuthenticateResponseSuccess );
		if( authSuccess?.SessionToken is not null ) {
			return authSuccess.SessionToken;
		}
		throw new BmxException( "Error authenticating Okta challenge MFA" );
	}

	async Task<OktaSession> IOktaApi.CreateSessionAsync( SessionOptions sessionOptions ) {
		var resp = await _httpClient.PostAsync( "sessions",
			new StringContent(
				JsonSerializer.Serialize( sessionOptions, SourceGenerationContext.Default.SessionOptions ),
				Encoding.Default,
				"application/json" ) );
		var session = await JsonSerializer.DeserializeAsync<OktaSession>( await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaSession );
		if( session is not null ) {
			return session;
		}
		throw new BmxException( "Error creating Okta session" );
	}

	async Task<OktaAccountState> IOktaApi.GetAccountsAsync( string accountType ) {
		var resp = await _httpClient.GetAsync(
			"users/me/appLinks" );

		var accounts = await JsonSerializer.DeserializeAsync<OktaApp[]>( await resp.Content.ReadAsStreamAsync(),
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

	async Task<string?> IOktaApi.GetMeResponseAsync( string sessionId ) {

		try {
			using var meResponse = await _httpClient.GetAsync( "users/me" );
			if( !meResponse.IsSuccessStatusCode ) {
				return null;
			}

			string meJson = await meResponse.Content.ReadAsStringAsync();
			var me = await meResponse.Content.ReadFromJsonAsync( SourceGenerationContext.Default.OktaMeResponse );
			return me?.Id;
		} catch( HttpRequestException ) {
			return null;
		} catch( JsonException ) {
			return null;
		}
	}

	private string ExtractAwsSaml( string htmlResponse ) {
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
