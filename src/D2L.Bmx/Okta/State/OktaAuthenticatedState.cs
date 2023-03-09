namespace D2L.Bmx;

public class OktaAuthenticatedState : IAuthenticatedState {
	public OktaAuthenticatedState( bool successfulAuthentication, string oktaSessionToken ) {
		SuccessfulAuthentication = successfulAuthentication;
		OktaSessionToken = oktaSessionToken;
	}

	public bool SuccessfulAuthentication { get; }
	internal string OktaSessionToken { get; }
}
