using System.Linq;
using Bmx.Core.State;
using Bmx.Idp.Okta.Models;

namespace Bmx.Idp.Okta.State {
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
}
