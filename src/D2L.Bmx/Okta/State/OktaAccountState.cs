using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta.State;

internal record OktaAccountState( OktaApp[] oktaApps, string accountType ) {

	public string[] Accounts => OktaApps.Where( app => app.AppName == AccountType ).Select( app => app.Label ).ToArray();
	public string AccountType => accountType;
	public OktaApp[] OktaApps => oktaApps;
}
