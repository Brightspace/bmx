using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Bmx.Core;
using Bmx.Idp.Okta.Models;
using Bmx.Idp.Okta.State;

namespace Bmx.Idp.Okta {
	public class OktaClient : IIdentityProvider<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState> {
		private readonly IOktaApi _oktaApi;

		public OktaClient( IOktaApi oktaApi ) {
			_oktaApi = oktaApi;
		}

		public string Name => "Okta";

		public void SetOrganization( string organization ) {
			_oktaApi.SetOrganization( organization );
		}

		public async Task<OktaAuthenticateState> Authenticate( string username, string password ) {
			var authResp = await _oktaApi.AuthenticateOkta( new AuthenticateOptions( username, password ) );
			return new OktaAuthenticateState( authResp.StateToken, authResp.Embedded.Factors );
		}

		public async Task<OktaAuthenticatedState> ChallengeMfa( OktaAuthenticateState state, int selectedMfaIndex,
			string challengeResponse ) {
			var mfaFactor = state.OktaMfaFactors[selectedMfaIndex];

			var authResp =
				await _oktaApi.AuthenticateChallengeMfaOkta(
					new AuthenticateChallengeMfaOptions( mfaFactor.Id, challengeResponse, state.OktaStateToken ) );

			// TODO: Check for auth successes and return state
			return new OktaAuthenticatedState( true, authResp.SessionToken );
		}

		public async Task<OktaAccountState> GetAccounts( OktaAuthenticatedState state, string accountType ) {
			// TODO: Use existing session if it exists in ~/.bmx and isn't expired
			var sessionResp = await _oktaApi.CreateSessionOkta( new SessionOptions( state.OktaSessionToken ) );
			// TODO: Consider making OktaAPI stateless as well (?)
			_oktaApi.AddSession( sessionResp.Id );

			var apps = await _oktaApi.GetAccountsOkta( sessionResp.UserId );
			return new OktaAccountState( apps, accountType );
		}

		public async Task<string> GetServiceProviderSaml( OktaAccountState state, int selectedAccountIndex ) {
			var account = state.OktaApps[selectedAccountIndex];
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
