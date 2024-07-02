using System.Text.Json.Serialization;

namespace D2L.Bmx.Okta.Models;

internal abstract record AuthenticateResponse {
	public record MfaRequired( string StateToken, OktaMfaFactor[] Factors ) : AuthenticateResponse;
	public record Success( string SessionToken ) : AuthenticateResponse;
}

internal record AuthenticateResponseRaw(
	DateTimeOffset ExpiresAt,
	[property: JsonConverter( typeof( JsonStringEnumConverter<AuthenticationStatus> ) )]
	AuthenticationStatus Status,
	string? StateToken,
	string? SessionToken,
	[property: JsonPropertyName( "_embedded" )]
	AuthenticateResponseEmbedded? Embedded
);

internal record AuthenticateResponseEmbedded(
	OktaMfaFactor[]? Factors
);

internal record OktaMfaQuestionProfile(
	string QuestionText
);

internal record OktaMfaFactor(
	string Id,
	string FactorType,
	string Provider,
	string VendorName,
	OktaMfaQuestionProfile Profile
);

internal enum AuthenticationStatus {
	UNKNOWN,
	PASSWORD_WARN,
	SUCCESS,
	LOCKED_OUT,
	MFA_REQUIRED,
	MFA_CHALLENGE,
}
