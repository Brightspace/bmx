namespace D2L.Bmx;

public class OktaAccountState : IAccountState {
	public OktaAccountState( OktaApp[] oktaApps, string accountType ) {
		OktaApps = oktaApps;
		AccountType = accountType;
		Accounts = OktaApps.Where( app => app.AppName == AccountType ).Select( app => app.Label ).ToArray();
	}

	public string[] Accounts { get; }
	internal string AccountType { get; }
	internal OktaApp[] OktaApps { get; }
}
