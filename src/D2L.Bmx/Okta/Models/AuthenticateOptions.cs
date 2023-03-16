using D2L.Bmx.Okta;
namespace D2L.Bmx.Okta.Models;

internal record AuthenticateOptions(
	string Username,
	string Password
);
