namespace D2L.Bmx.Okta.Models;

internal record AuthenticateRequest(
	string Username,
	string Password
);
