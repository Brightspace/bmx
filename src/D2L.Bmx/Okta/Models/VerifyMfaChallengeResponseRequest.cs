namespace D2L.Bmx.Okta.Models;

internal record VerifyMfaChallengeResponseRequest(
	string StateToken,
	string PassCode
);
