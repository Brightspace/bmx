using System.Linq;
using Bmx.Core.State;
using Bmx.Idp.Okta.Models;

namespace Bmx.Idp.Okta.State {
	public class OktaAccountState : IAccountState {
		public OktaAccountState( OktaApp[] oktaApps, string accountType ) {
			OktaApps = oktaApps;
			AccountType = accountType;
		}

		public string[] Accounts {
			get => OktaApps.Where( app => app.AppName == AccountType ).Select( app => app.Label ).ToArray();
		}

		internal string AccountType { get; }
		internal OktaApp[] OktaApps { get; }
	}
}
