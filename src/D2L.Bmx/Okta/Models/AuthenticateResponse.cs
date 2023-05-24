using System.Text.Json.Serialization;

namespace D2L.Bmx.Okta.Models;

internal record AuthenticateResponseInital(
	DateTimeOffset ExpiresAt,
	[property: JsonConverter( typeof( JsonStringEnumConverter ) )]
	AuthenticationStatus Status,
	string? StateToken,
	string? SessionToken,
	[property: JsonPropertyName( "_embedded" )]
	AuthenticateResponseEmbeddedInitial? Embedded
);

internal record AuthenticateResponseSuccess(
	DateTimeOffset ExpiresAt,
	[property: JsonConverter( typeof( JsonStringEnumConverter ) )]
	AuthenticationStatus Status,
	string? SessionToken
);

internal struct AuthenticateResponseEmbeddedInitial {
	public OktaMfaFactor[]? Factors { get; set; }
}

internal record OktaMfaFactor(
	string Id,
	string FactorType,
	string Provider,
	string VendorName
);

internal enum AuthenticationStatus {
	UNKNOWN,
	PASSWORD_WARN,
	SUCCESS,
	LOCKED_OUT,
	MFA_REQUIRED,
	MFA_CHALLENGE,
}
