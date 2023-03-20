using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta.State;

internal record OktaAuthenticateState( string OktaStateToken, OktaMfaFactor[] OktaMfaFactors ) {

	public MfaOption[] MfaOptions => this.OktaMfaFactors.Select( factor => {
		if( factor.FactorType.Contains( "token" ) || factor.FactorType.Contains( "sms" ) ) {
			return new MfaOption( factor.FactorType, MfaType.Challenge );
		}

		if( factor.FactorType.Contains( "push" ) ) {
			return new MfaOption( factor.FactorType, MfaType.Verify );
		}

		return new MfaOption( factor.FactorType, MfaType.Unknown );
	} ).ToArray();
	// Store auth state for later steps (MFA challenge verify etc...)
	public string OktaStateToken = OktaStateToken;
	public OktaMfaFactor[] OktaMfaFactors = OktaMfaFactors;
}
