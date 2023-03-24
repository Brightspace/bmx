namespace D2L.Bmx.Okta.Models;

internal record AuthenticateChallengeMfaOptions(
	string FactorId,
	string PassCode,
	string StateToken
);
