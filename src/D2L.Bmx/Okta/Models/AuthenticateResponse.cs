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

[JsonPolymorphic(
	TypeDiscriminatorPropertyName = "factorType",
	IgnoreUnrecognizedTypeDiscriminators = true
)]
[JsonDerivedType( typeof( OktaMfaQuestionFactor ), "question" )]
[JsonDerivedType( typeof( OktaMfaHardwareToken ), "token" )]
[JsonDerivedType( typeof( OktaMfaHardwareTokenFactor ), "token:hardware" )]
[JsonDerivedType( typeof( OktaMfaSoftwareTotpFactor ), "token:software:totp" )]
[JsonDerivedType( typeof( OktaMfaHotpFactor ), "token:hotp" )]
[JsonDerivedType( typeof( OktaMfaSmsFactor ), "sms" )]
[JsonDerivedType( typeof( OktaMfaCallFactor ), "call" )]
[JsonDerivedType( typeof( OktaMfaEmailFactor ), "email" )]
internal record OktaMfaFactor {
	public required string Id { get; set; }
	public required string Provider { get; set; }
	public required string VendorName { get; set; }

	[JsonIgnore]
	public virtual string FactorName => "Unknown";
	[JsonIgnore]
	public virtual bool RequireChallengeIssue => false;
}

internal record OktaMfaQuestionFactor() : OktaMfaFactor() {
	public override string FactorName => "Security Question";
	public required OktaMfaQuestionProfile Profile { get; set; }
}

internal record OktaMfaQuestionProfile(
	string QuestionText
);

internal record OktaMfaHardwareToken() : OktaMfaFactor {
	public override string FactorName => "Hardware Token";
}

internal record OktaMfaHardwareTokenFactor() : OktaMfaFactor {
	public override string FactorName => "Hardware TOTP";
}

internal record OktaMfaSoftwareTotpFactor() : OktaMfaFactor {
	public override string FactorName => "Software TOTP";
}

internal record OktaMfaHotpFactor() : OktaMfaFactor {
	public override string FactorName => "HOTP";
}

internal record OktaMfaSmsFactor() : OktaMfaFactor {
	public override string FactorName => "SMS";
	public override bool RequireChallengeIssue => true;
}

internal record OktaMfaCallFactor() : OktaMfaFactor {
	public override string FactorName => "Call";
	public override bool RequireChallengeIssue => true;
}

internal record OktaMfaEmailFactor() : OktaMfaFactor {
	public override string FactorName => "Email";
	public override bool RequireChallengeIssue => true;
}

internal enum AuthenticationStatus {
	UNKNOWN,
	PASSWORD_WARN,
	SUCCESS,
	LOCKED_OUT,
	MFA_REQUIRED,
	MFA_CHALLENGE,
}
