using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta.State;

internal class OktaAccountState {
	public OktaAccountState( OktaApp[] oktaApps, string accountType ) {
		OktaApps = oktaApps;
		AccountType = accountType;
		Accounts = OktaApps.Where( app => app.AppName == AccountType ).Select( app => app.Label ).ToArray();
	}

	public string[] Accounts { get; }
	public string AccountType { get; }
	public OktaApp[] OktaApps { get; }
}
