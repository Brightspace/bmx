using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta.State;

internal record OktaAuthenticateState(
string? OktaStateToken,
string? OktaSessionToken,
OktaMfaFactor[] OktaMfaFactors ) {

	public MfaOption[] MfaOptions => this.OktaMfaFactors.Select( factor => {
		if( factor.FactorType.Contains( "token" ) ) {
			return new MfaOption( Name: factor.FactorType, Provider: factor.Provider, MfaType.Challenge );
		} else if( factor.FactorType.Contains( "sms" ) ) {
			return new MfaOption( Name: factor.FactorType, Provider: factor.Provider, MfaType.Sms );
		} else if( factor.FactorType.Contains( "question" ) ) {
			return new MfaOption( Name: factor.FactorType, Provider: factor.Provider, MfaType.Question );
		}
		return new MfaOption( Name: factor.FactorType, Provider: factor.Provider, MfaType.Unknown );
	} ).ToArray();
	// Store auth state for later steps (MFA challenge verify etc...)
	public string? OktaStateToken = OktaStateToken;
	public string? OktaSessionToken = OktaSessionToken;
	public OktaMfaFactor[] OktaMfaFactors = OktaMfaFactors;
}
