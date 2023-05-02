namespace D2L.Bmx.Okta.State;

internal record OktaAuthenticatedState(
	bool SuccessfulAuthentication,
	string OktaSessionId
);
