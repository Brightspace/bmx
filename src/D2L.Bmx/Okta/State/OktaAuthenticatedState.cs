namespace D2L.Bmx.Okta.State;

internal class OktaAuthenticatedState {
	internal OktaAuthenticatedState( bool successfulAuthentication, string oktaSessionToken ) {
		SuccessfulAuthentication = successfulAuthentication;
		OktaSessionToken = oktaSessionToken;
	}

	internal bool SuccessfulAuthentication { get; }
	internal string OktaSessionToken { get; }
}
