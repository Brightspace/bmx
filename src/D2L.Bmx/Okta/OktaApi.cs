using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta;

internal interface IOktaApi {
	void SetOrganization( string organization );
	void AddSession( string sessionId );
	Task<AuthenticateResponseInital> AuthenticateOktaAsync( AuthenticateOptions authOptions );
	Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOktaAsync( AuthenticateChallengeMfaOptions authOptions );
	Task<OktaSession> CreateSessionOktaAsync( SessionOptions sessionOptions );
	Task<OktaApp[]> GetAccountsOktaAsync( string userId );
	Task<string> GetAccountOktaAsync( Uri linkUri );
}

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof( AuthenticateOptions ) )]
[JsonSerializable( typeof( AuthenticateChallengeMfaOptions ) )]
[JsonSerializable( typeof( SessionOptions ) )]
[JsonSerializable( typeof( AuthenticateResponseInital ) )]
[JsonSerializable( typeof( AuthenticateResponseSuccess ) )]
[JsonSerializable( typeof( OktaSession ) )]
[JsonSerializable( typeof( OktaApp[] ) )]
internal partial class SourceGenerationContext : JsonSerializerContext {
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

	async Task<AuthenticateResponseInital> IOktaApi.AuthenticateOktaAsync( AuthenticateOptions authOptions ) {
		var resp = await _httpClient.PostAsync( "authn",
			new StringContent(
				JsonSerializer.Serialize( authOptions!, SourceGenerationContext.Default.AuthenticateOptions ),
				Encoding.Default,
				"application/json" ) );
		var authInitial = await JsonSerializer.DeserializeAsync(
			await resp.Content.ReadAsStreamAsync(), SourceGenerationContext.Default.AuthenticateResponseInital );
		if( authInitial is not null ) {
			return authInitial;
		}
		throw new BmxException( "Error authenticating Okta" );
	}

	async Task<AuthenticateResponseSuccess> IOktaApi.AuthenticateChallengeMfaOktaAsync(
		AuthenticateChallengeMfaOptions authOptions ) {
		var resp = await _httpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
			new StringContent(
				JsonSerializer.Serialize( authOptions!, SourceGenerationContext.Default.AuthenticateChallengeMfaOptions ),
				Encoding.Default,
				"application/json" ) );
		var authSuccess = await JsonSerializer.DeserializeAsync<AuthenticateResponseSuccess>(
			await resp.Content.ReadAsStreamAsync(), SourceGenerationContext.Default.AuthenticateResponseSuccess );
		if( authSuccess is not null ) {
			return authSuccess;
		}
		throw new BmxException( "Error authenticating Okta challenge MFA" );
	}

	async Task<OktaSession> IOktaApi.CreateSessionOktaAsync( SessionOptions sessionOptions ) {
		var resp = await _httpClient.PostAsync( "sessions",
			new StringContent(
				JsonSerializer.Serialize( sessionOptions!, SourceGenerationContext.Default.SessionOptions ),
				Encoding.Default,
				"application/json" ) );
		var session = await JsonSerializer.DeserializeAsync<OktaSession>( await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaSession );
		if( session is not null ) {
			return session;
		}
		throw new BmxException( "Error creating Okta session" );
	}

	async Task<OktaApp[]> IOktaApi.GetAccountsOktaAsync( string userId ) {
		var resp = await _httpClient.GetAsync( $"users/{userId}/appLinks" );
		var accounts = await JsonSerializer.DeserializeAsync<OktaApp[]>( await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaAppArray );
		if( accounts is not null ) {
			return accounts;
		}
		throw new BmxException( "Error retrieving Okta accounts" );
	}

	async Task<string> IOktaApi.GetAccountOktaAsync( Uri linkUri ) {
		var resp = await _httpClient.GetAsync( linkUri );
		return await resp.Content.ReadAsStringAsync();
	}
}
