using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Bmx.Core;
using Bmx.Idp.Okta.Models;

namespace Bmx.Idp.Okta {
	public class OktaClient : IIdentityProvider {
		private readonly IOktaApi _oktaApi;
		private string _oktaStateToken;
		private string _oktaSessionToken;
		private OktaMfaFactor[] _oktaMfaFactors;
		private OktaApp[] _oktaApps;

		public OktaClient( IOktaApi oktaApi ) {
			_oktaApi = oktaApi;
		}

		public string Name => "Okta";

		public void SetOrganization( string organization ) {
			_oktaApi.SetOrganization( organization );
		}

		public async Task<MfaOption[]> Authenticate( string username, string password ) {
			var authResp = await _oktaApi.AuthenticateOkta( new AuthenticateOptions( username, password ) );

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
				await _oktaApi.AuthenticateChallengeMfaOkta(
					new AuthenticateChallengeMfaOptions( mfaFactor.Id, challengeResponse, _oktaStateToken ) );

			_oktaSessionToken = authResp.SessionToken;

			// TODO: Check for auth successes and return state
			return true;
		}

		public async Task<string[]> GetAccounts( string accountType ) {
			// TODO: Use existing session if it exists in ~/.bmx and isn't expired
			var sessionResp = await _oktaApi.CreateSessionOkta( new SessionOptions( _oktaSessionToken ) );
			_oktaApi.AddSession( sessionResp.Id );

			_oktaApps = await _oktaApi.GetAccountsOkta( sessionResp.UserId );
			return _oktaApps.Where( app => app.AppName == accountType ).Select( app => app.Label ).ToArray();
		}

		public async Task<string> GetServiceProviderSaml( int selectedAccountIndex ) {
			var account = _oktaApps[selectedAccountIndex];
			var accountPage = await _oktaApi.GetAccountOkta( new Uri( account.LinkUrl ) );
			return ExtractAwsSaml( accountPage );
		}


		private string ExtractAwsSaml( string htmlResponse ) {
			// HTML page is fairly malformed, grab just the <input> with the SAML data for further processing
			var inputRegexPattern = "<input name=\"SAMLResponse\" type=\"hidden\" value=\".*?\"/>";
			var inputRegex = new Regex( inputRegexPattern, RegexOptions.Compiled );

			// Access the SAML data from the parsed <input>
			var inputXml = new XmlDocument();
			inputXml.LoadXml( inputRegex.Match( htmlResponse ).Value );

			return inputXml.SelectSingleNode( "//@value" ).InnerText;
		}
	}
}
