namespace D2L.Bmx;

public class OktaAuthenticateState : IAuthenticateState {
	public OktaAuthenticateState( string oktaStateToken, OktaMfaFactor[] oktaMfaFactors ) {
		OktaStateToken = oktaStateToken;
		OktaMfaFactors = oktaMfaFactors;
		MfaOptions = OktaMfaFactors.Select( factor => {
			if( factor.FactorType.Contains( "token" ) || factor.FactorType.Contains( "sms" ) ) {
				return new MfaOption( factor.FactorType, MfaType.Challenge );
			}

			if( factor.FactorType.Contains( "push" ) ) {
				return new MfaOption( factor.FactorType, MfaType.Verify );
			}

			return new MfaOption( factor.FactorType, MfaType.Unknown );
		} ).ToArray();
	}

	public MfaOption[] MfaOptions { get; }
	// Store auth state for later steps (MFA challenge verify etc...)
	internal string OktaStateToken { get; }
	internal OktaMfaFactor[] OktaMfaFactors { get; }
}
