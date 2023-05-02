namespace D2L.Bmx.Okta.Models;
internal record OktaSessionCache(
	string UserId,
	string Org,
	string SessionId,
	DateTimeOffset ExpiresAt
);
