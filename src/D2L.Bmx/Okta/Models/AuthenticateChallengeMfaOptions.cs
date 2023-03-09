namespace D2L.Bmx;
public struct AuthenticateChallengeMfaOptions {
	public string FactorId { get; set; }
	public string PassCode { get; set; }
	public string StateToken { get; set; }

	public AuthenticateChallengeMfaOptions( string factorId, string passCode, string stateToken ) {
		FactorId = factorId;
		PassCode = passCode;
		StateToken = stateToken;
	}
}
