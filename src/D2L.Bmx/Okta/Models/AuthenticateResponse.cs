using System.Text.Json.Serialization;
namespace D2L.Bmx.Okta.Models;

internal class AuthenticateResponse {
	public DateTimeOffset ExpiresAt { get; set; }

	[JsonConverter( typeof( JsonStringEnumConverter ) )]
	public AuthenticationStatus Status { get; set; }
}

internal class AuthenticateResponseInital : AuthenticateResponse {
	public string? StateToken { get; set; }
	[JsonPropertyName( "_embedded" )] public AuthenticateResponseEmbeddedInitial Embedded { get; set; }
}

internal class AuthenticateResponseSuccess : AuthenticateResponse {
	public string? SessionToken { get; set; }
}

internal struct AuthenticateResponseEmbeddedInitial {
	public OktaMfaFactor[] Factors { get; set; }
}

internal enum AuthenticationStatus {
	UNKNOWN,
	PASSWORD_WARN,
	SUCCESS,
	LOCKED_OUT,
	MFA_REQUIRED,
	MFA_CHALLENGE,
}
