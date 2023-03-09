using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace D2L.Bmx;

public interface IOktaApi {
	void SetOrganization( string organization );
	void AddSession( string sessionId );
	Task<AuthenticateResponseInital> AuthenticateOkta( AuthenticateOptions authOptions );
	Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOkta( AuthenticateChallengeMfaOptions authOptions );
	Task<OktaSession> CreateSessionOkta( SessionOptions sessionOptions );
	Task<OktaApp[]> GetAccountsOkta( string userId );
	Task<string> GetAccountOkta( Uri linkUri );
}

public class OktaApi : IOktaApi {
	private readonly JsonSerializerOptions _serializeOptions =
		new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, };

	private readonly CookieContainer _cookieContainer;
	private readonly HttpClient _httpClient;

	public OktaApi() {
		_cookieContainer = new CookieContainer();
		_httpClient = new HttpClient( new HttpClientHandler { CookieContainer = _cookieContainer } );
		_httpClient.Timeout = TimeSpan.FromSeconds( 30 );
		_httpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
	}

	public void SetOrganization( string organization ) {
		_httpClient.BaseAddress = new Uri( $"https://{organization}.okta.com/api/v1/" );
	}

	public void AddSession( string sessionId ) {
		_cookieContainer.Add( new Cookie( "sid", sessionId, "/", _httpClient.BaseAddress.Host ) );
	}

	public async Task<AuthenticateResponseInital> AuthenticateOkta( AuthenticateOptions authOptions ) {
		var resp = await _httpClient.PostAsync( "authn",
			new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ), Encoding.Default,
				"application/json" ) );

		return await JsonSerializer.DeserializeAsync<AuthenticateResponseInital>(
			await resp.Content.ReadAsStreamAsync(), _serializeOptions );
	}

	public async Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOkta(
		AuthenticateChallengeMfaOptions authOptions ) {
		var resp = await _httpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
			new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ), Encoding.Default,
				"application/json" ) );
		return await JsonSerializer.DeserializeAsync<AuthenticateResponseSuccess>(
			await resp.Content.ReadAsStreamAsync(), _serializeOptions );
	}

	public async Task<OktaSession> CreateSessionOkta( SessionOptions sessionOptions ) {
		var resp = await _httpClient.PostAsync( "sessions",
			new StringContent( JsonSerializer.Serialize( sessionOptions, _serializeOptions ), Encoding.Default,
				"application/json" ) );
		return await JsonSerializer.DeserializeAsync<OktaSession>( await resp.Content.ReadAsStreamAsync(),
			_serializeOptions );
	}

	public async Task<OktaApp[]> GetAccountsOkta( string userId ) {
		var resp = await _httpClient.GetAsync( $"users/{userId}/appLinks" );
		return await JsonSerializer.DeserializeAsync<OktaApp[]>( await resp.Content.ReadAsStreamAsync(),
			_serializeOptions );
	}

	public async Task<string> GetAccountOkta( Uri linkUri ) {
		var resp = await _httpClient.GetAsync( linkUri );
		return await resp.Content.ReadAsStringAsync();
	}
}
