using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bmx.Core;
using Bmx.Idp.Okta.models;

namespace Bmx.Idp.Okta {
	public class OktaClient : IIdentityProvider {
		private readonly JsonSerializerOptions _serializeOptions =
			new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase,};

		private static readonly CookieContainer CookieContainer = new CookieContainer();

		private static readonly HttpClient HttpClient = new HttpClient( new HttpClientHandler {
			CookieContainer = CookieContainer
		} );

		private string _oktaStateToken;
		private string _oktaSessionToken;
		private OktaMfaFactor[] _oktaMfaFactors;
		private OktaApp[] _oktaApps;

		public OktaClient( string organization ) {
			HttpClient.BaseAddress = new Uri( $"https://{organization}.okta.com/api/v1/" );
			HttpClient.Timeout = TimeSpan.FromSeconds( 30 );
			HttpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
		}

		public string Name => "Okta";

		public async Task<MfaOption[]> Authenticate( string username, string password ) {
			var authResp = await AuthenticateOkta( new AuthenticateOptions( username, password ) );

			// Store auth state for later steps (MFA challenge verify etc...)
			_oktaStateToken = authResp.StateToken;
			_oktaMfaFactors = authResp.Embedded.Factors;

			return _oktaMfaFactors.Select( factor => {
				if( factor.FactorType.Contains( "token" ) || factor.FactorType.Contains( "sms" ) ) {
					return new MfaOption( factor.FactorType, MfaType.Challenge );
				}

				if( factor.FactorType.Contains( "push" ) ) {
					return new MfaOption( factor.FactorType, MfaType.Verify );
				}

				return new MfaOption( factor.FactorType, MfaType.Unknown );
			} ).ToArray();
		}

		public async Task<bool> ChallengeMfa( int selectedMfaIndex, string challengeResponse ) {
			var mfaFactor = _oktaMfaFactors[selectedMfaIndex];

			var authResp =
				await AuthenticateChallengeMfaOkta(
					new AuthenticateChallengeMfaOptions( mfaFactor.Id, challengeResponse, _oktaStateToken ) );

			_oktaSessionToken = authResp.SessionToken;

			// TODO: Check for auth successes and return state
			return true;
		}

		public async Task<string[]> GetAccounts( string accountType ) {
			// TODO: Use existing session if it exists in ~/.bmx and isn't expired
			var sessionResp = await CreateSessionOkta( new SessionOptions( _oktaSessionToken ) );
			AddSession( sessionResp.Id );

			_oktaApps = await GetAccountsOkta( sessionResp.UserId );
			return _oktaApps.Where( app => app.AppName == accountType ).Select( app => app.Label ).ToArray();
		}

		private void AddSession( string sessionId ) {
			CookieContainer.Add( new Cookie( "sid", sessionId, "/", HttpClient.BaseAddress.Host ) );
		}

		// TODO: Consider consolidating this kind of thing, ex: to a OktaHttpClient
		private async Task<AuthenticateResponseInital> AuthenticateOkta( AuthenticateOptions authOptions ) {
			var resp = await HttpClient.PostAsync( "authn",
				new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ), Encoding.Default,
					"application/json" ) );

			return await JsonSerializer.DeserializeAsync<AuthenticateResponseInital>(
				await resp.Content.ReadAsStreamAsync(), _serializeOptions );
		}

		private async Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOkta(
			AuthenticateChallengeMfaOptions authOptions ) {
			var resp = await HttpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
				new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ), Encoding.Default,
					"application/json" ) );
			return await JsonSerializer.DeserializeAsync<AuthenticateResponseSuccess>(
				await resp.Content.ReadAsStreamAsync(), _serializeOptions );
		}

		private async Task<OktaSession> CreateSessionOkta( SessionOptions sessionOptions ) {
			var resp = await HttpClient.PostAsync( "sessions",
				new StringContent( JsonSerializer.Serialize( sessionOptions, _serializeOptions ), Encoding.Default,
					"application/json" ) );
			return await JsonSerializer.DeserializeAsync<OktaSession>( await resp.Content.ReadAsStreamAsync(),
				_serializeOptions );
		}

		private async Task<OktaApp[]> GetAccountsOkta( string userId ) {
			var resp = await HttpClient.GetAsync( $"users/{userId}/appLinks" );
			return await JsonSerializer.DeserializeAsync<OktaApp[]>( await resp.Content.ReadAsStreamAsync(),
				_serializeOptions );
		}
	}
}
