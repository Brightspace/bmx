using System.Linq;
using Bmx.Core;
using Bmx.Core.State;
using Bmx.Idp.Okta.Models;

namespace Bmx.Idp.Okta.State {
	public class OktaAuthenticateState : IAuthenticateState {
		public OktaAuthenticateState( string oktaStateToken, OktaMfaFactor[] oktaMfaFactors ) {
			OktaStateToken = oktaStateToken;
			OktaMfaFactors = oktaMfaFactors;
		}

		public MfaOption[] MfaOptions {
			get => OktaMfaFactors.Select( factor => {
				if( factor.FactorType.Contains( "token" ) || factor.FactorType.Contains( "sms" ) ) {
					return new MfaOption( factor.FactorType, MfaType.Challenge );
				}

				if( factor.FactorType.Contains( "push" ) ) {
					return new MfaOption( factor.FactorType, MfaType.Verify );
				}

				return new MfaOption( factor.FactorType, MfaType.Unknown );
			} ).ToArray();
		}

		// Store auth state for later steps (MFA challenge verify etc...)
		internal string OktaStateToken { get; }
		internal OktaMfaFactor[] OktaMfaFactors { get; }
	}
}
