namespace D2L.Bmx.Okta.State;

public class OktaAuthenticatedState {
	public OktaAuthenticatedState( bool successfulAuthentication, string oktaSessionToken ) {
		SuccessfulAuthentication = successfulAuthentication;
		OktaSessionToken = oktaSessionToken;
	}

	public bool SuccessfulAuthentication { get; }
	internal string OktaSessionToken { get; }
}
