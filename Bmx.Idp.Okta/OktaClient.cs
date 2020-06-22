using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Bmx.Core;
using Bmx.Idp.Okta.models;

namespace Bmx.Idp.Okta {
	public class OktaClient : IIdentityProvider {
		private readonly JsonSerializerOptions _serializeOptions =
			new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase,};

		private static readonly HttpClient HttpClient = new HttpClient();

		private readonly Dictionary<AuthenticationStatus, AuthenticateResponse> _authenticateResponses =
			new Dictionary<AuthenticationStatus, AuthenticateResponse>();

		public OktaClient( string organization ) {
			HttpClient.BaseAddress = new Uri( $"https://{organization}.okta.com/api/v1/" );
			HttpClient.Timeout = TimeSpan.FromSeconds( 30 );
			HttpClient.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
		}

		public string Name => "Okta";

		public async Task<MfaOption[]> Authenticate( string username, string password ) {
			var authResp = await AuthenticateOkta( new AuthenticateOptions( username, password ) );

			// Store auth state for later steps (MFA challenge verify etc...)
			_authenticateResponses.Add( AuthenticationStatus.MFA_REQUIRED, authResp );

			return authResp.Embedded.Factors.Select( factor => {
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
			var stateToken = ( (AuthenticateResponseInital)_authenticateResponses[AuthenticationStatus.MFA_REQUIRED] )
				.StateToken;
			var mfaFactor =
				( (AuthenticateResponseInital)_authenticateResponses[AuthenticationStatus.MFA_REQUIRED] ).Embedded
				.Factors[selectedMfaIndex];

			var authResp =
				await AuthenticateChallengeMfaOkta(
					new AuthenticateChallengeMfaOptions( mfaFactor.Id, challengeResponse, stateToken ) );

			_authenticateResponses.Add( AuthenticationStatus.SUCCESS, authResp );

			// TODO: Check for auth successes and return state
			return true;
			// TODO: Implicitly Call Session here
		}

		// TODO: Consider consolidating this kind of thing, ex: to a OktaHttpClient
		private async Task<AuthenticateResponseInital> AuthenticateOkta( AuthenticateOptions authOptions ) {
			var resp = await HttpClient.PostAsync( "authn",
				new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ) ) );
			return await JsonSerializer.DeserializeAsync<AuthenticateResponseInital>(
				await resp.Content.ReadAsStreamAsync(), _serializeOptions );
		}

		private async Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOkta(
			AuthenticateChallengeMfaOptions authOptions ) {
			var resp = await HttpClient.PostAsync( $"authn/factors/{authOptions.FactorId}/verify",
				new StringContent( JsonSerializer.Serialize( authOptions, _serializeOptions ) ) );
			return await JsonSerializer.DeserializeAsync<AuthenticateResponseSuccess>(
				await resp.Content.ReadAsStreamAsync(), _serializeOptions );
		}
	}
}
