namespace D2L.Bmx.Okta.State;

internal class OktaAuthenticatedState {
	public OktaAuthenticatedState( bool successfulAuthentication, string oktaSessionToken ) {
		SuccessfulAuthentication = successfulAuthentication;
		OktaSessionToken = oktaSessionToken;
	}

	public bool SuccessfulAuthentication { get; }
	internal string OktaSessionToken { get; }
}
