using System.Text.RegularExpressions;
using System.Xml;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;
namespace D2L.Bmx.Okta;

internal interface IOktaClient {
	public string Name { get; }
	void SetOrganization( string organization );
	Task<OktaAuthenticateState> AuthenticateAsync( string username, string password );

	Task<OktaAuthenticatedState> ChallengeMfaAsync( OktaAuthenticateState state, int selectedMfaIndex,
		string challengeResponse );

	Task<OktaAccountState> GetAccountsAsync( OktaAuthenticatedState state, string accountType );
	Task<string> GetServiceProviderSamlAsync( OktaAccountState state, int selectedAccountIndex );
}
internal class OktaClient : IOktaClient {
	private readonly IOktaApi _oktaApi;

	public OktaClient( IOktaApi oktaApi ) {
		_oktaApi = oktaApi;
	}

	public string Name => "Okta";

	public void SetOrganization( string organization ) {
		_oktaApi.SetOrganization( organization );
	}

	public async Task<OktaAuthenticateState> AuthenticateAsync( string username, string password ) {
		var authResp = await _oktaApi.AuthenticateOktaAsync( new AuthenticateOptions( username, password ) );
		return new OktaAuthenticateState( authResp.StateToken, authResp.Embedded.Factors );
	}

	public async Task<OktaAuthenticatedState> ChallengeMfaAsync( OktaAuthenticateState state, int selectedMfaIndex,
		string challengeResponse ) {
		var mfaFactor = state.OktaMfaFactors[selectedMfaIndex];

		var authResp =
			await _oktaApi.AuthenticateChallengeMfaOktaAsync(
				new AuthenticateChallengeMfaOptions( mfaFactor.Id, challengeResponse, state.OktaStateToken ) );

		// TODO: Check for auth successes and return state
		return new OktaAuthenticatedState( true, authResp.SessionToken );
	}

	public async Task<OktaAccountState> GetAccountsAsync( OktaAuthenticatedState state, string accountType ) {
		// TODO: Use existing session if it exists in ~/.bmx and isn't expired
		var sessionResp = await _oktaApi.CreateSessionOktaAsync( new SessionOptions( state.OktaSessionToken ) );
		// TODO: Consider making OktaAPI stateless as well (?)
		_oktaApi.AddSession( sessionResp.Id );

		var apps = await _oktaApi.GetAccountsOktaAsync( sessionResp.UserId );
		return new OktaAccountState( apps, accountType );
	}

	public async Task<string> GetServiceProviderSamlAsync( OktaAccountState state, int selectedAccountIndex ) {
		var account = state.OktaApps[selectedAccountIndex];
		var accountPage = await _oktaApi.GetAccountOktaAsync( new Uri( account.LinkUrl ) );
		return ExtractAwsSaml( accountPage );
	}


	private string ExtractAwsSaml( string htmlResponse ) {
		// HTML page is fairly malformed, grab just the <input> with the SAML data for further processing
		var inputRegexPattern = "<input name=\"SAMLResponse\" type=\"hidden\" value=\".*?\"/>";
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
