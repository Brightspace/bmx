using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta.State;

internal record OktaAuthenticateState(
	string? OktaStateToken,
	string? OktaSessionToken,
	OktaMfaFactor[]? OktaMfaFactors
);
