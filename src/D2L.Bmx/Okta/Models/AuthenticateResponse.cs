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
[JsonDerivedType( typeof( OktaMfaQuestionFactor ), OktaMfaQuestionFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaTokenFactor ), OktaMfaTokenFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaHardwareTokenFactor ), OktaMfaHardwareTokenFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaSoftwareTotpFactor ), OktaMfaSoftwareTotpFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaHotpFactor ), OktaMfaHotpFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaSmsFactor ), OktaMfaSmsFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaCallFactor ), OktaMfaCallFactor.FactorType )]
[JsonDerivedType( typeof( OktaMfaEmailFactor ), OktaMfaEmailFactor.FactorType )]
internal record OktaMfaFactor {
	public required string Id { get; set; }
	public required string Provider { get; set; }
	public required string VendorName { get; set; }

	[JsonIgnore]
	public virtual string FactorName => "Unknown";
	[JsonIgnore]
	public virtual bool RequireChallengeIssue => false;
}

internal record OktaMfaQuestionFactor(
	OktaMfaQuestionProfile Profile
) : OktaMfaFactor {
	public const string FactorType = "question";
	public override string FactorName => "Security Question";
}

internal record OktaMfaQuestionProfile(
	string QuestionText
);

internal record OktaMfaTokenFactor() : OktaMfaFactor {
	public const string FactorType = "token";
	public override string FactorName => "Token";
}

internal record OktaMfaHardwareTokenFactor() : OktaMfaFactor {
	public const string FactorType = "token:hardware";
	public override string FactorName => "Hardware Token";
}

internal record OktaMfaSoftwareTotpFactor() : OktaMfaFactor {
	public const string FactorType = "token:software:totp";
	public override string FactorName => "Software TOTP";
}

internal record OktaMfaHotpFactor() : OktaMfaFactor {
	public const string FactorType = "token:hotp";
	public override string FactorName => "HOTP";
}

internal record OktaMfaSmsFactor() : OktaMfaFactor {
	public const string FactorType = "sms";
	public override string FactorName => "SMS";
	public override bool RequireChallengeIssue => true;
}

internal record OktaMfaCallFactor() : OktaMfaFactor {
	public const string FactorType = "call";
	public override string FactorName => "Call";
	public override bool RequireChallengeIssue => true;
}

internal record OktaMfaEmailFactor() : OktaMfaFactor {
	public const string FactorType = "email";
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
