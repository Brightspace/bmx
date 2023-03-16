namespace D2L.Bmx.Okta.Models;

internal record OktaSession(
	string Id,
	string UserId,
	DateTimeOffset CreatedAt,
	DateTimeOffset ExpiresAt
);
