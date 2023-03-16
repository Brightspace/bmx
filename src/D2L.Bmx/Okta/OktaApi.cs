using System.Diagnostics.CodeAnalysis;
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

[JsonSerializable( typeof( AuthenticateResponseInital ) )]
[JsonSerializable( typeof( AuthenticateResponseSuccess ) )]
[JsonSerializable( typeof( OktaSession ) )]
[JsonSerializable( typeof( OktaApp[] ) )]
internal partial class SourceGenerationContext : JsonSerializerContext {
}
internal class OktaApi : IOktaApi {
	private readonly JsonSerializerOptions _serializeOptions =
		new JsonSerializerOptions {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

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

	[RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	[RequiresUnreferencedCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	public async Task<AuthenticateResponseInital> AuthenticateOktaAsync( AuthenticateOptions authOptions ) {
		var resp = await _httpClient.PostAsync( "authn",
			new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ), Encoding.Default,
				"application/json" ) );
		return await JsonSerializer.DeserializeAsync(
			await resp.Content.ReadAsStreamAsync()!, SourceGenerationContext.Default.AuthenticateResponseInital );
	}

	[RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	[RequiresUnreferencedCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	public async Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOktaAsync(
		AuthenticateChallengeMfaOptions authOptions ) {
		var resp = await _httpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
			new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ), Encoding.Default,
				"application/json" ) );
		return await JsonSerializer.DeserializeAsync<AuthenticateResponseSuccess>(
			await resp.Content.ReadAsStreamAsync(), SourceGenerationContext.Default.AuthenticateResponseSuccess );
	}

	[RequiresUnreferencedCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	public async Task<OktaSession> CreateSessionOktaAsync( SessionOptions sessionOptions ) {
		var resp = await _httpClient.PostAsync( "sessions",
			new StringContent( JsonSerializer.Serialize( sessionOptions, _serializeOptions ), Encoding.Default,
				"application/json" ) );
		return await JsonSerializer.DeserializeAsync<OktaSession>( await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaSession );
	}

	public async Task<OktaApp[]> GetAccountsOktaAsync( string userId ) {
		var resp = await _httpClient.GetAsync( $"users/{userId}/appLinks" );
		return await JsonSerializer.DeserializeAsync<OktaApp[]>( await resp.Content.ReadAsStreamAsync(),
			SourceGenerationContext.Default.OktaAppArray );
	}

	public async Task<string> GetAccountOktaAsync( Uri linkUri ) {
		var resp = await _httpClient.GetAsync( linkUri );
		return await resp.Content.ReadAsStringAsync();
	}
}
