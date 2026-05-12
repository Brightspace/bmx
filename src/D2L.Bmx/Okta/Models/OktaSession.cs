namespace D2L.Bmx.Okta.Models;

internal record OktaSession(
	string Id,
	string Login,
	string UserId,
	string Status,
	DateTimeOffset CreatedAt,
	DateTimeOffset ExpiresAt
);
