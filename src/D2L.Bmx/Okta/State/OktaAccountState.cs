using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta.State;

internal record OktaAccountState( OktaApp[] OktaApps, string AccountType ) {

	public string[] Accounts => this.OktaApps.Where( app => app.AppName == AccountType ).Select( app => app.Label ).ToArray();
	public string AccountType = AccountType;
	public OktaApp[] OktaApps = OktaApps;
}
