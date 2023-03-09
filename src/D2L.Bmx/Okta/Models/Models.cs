using System;
using System.Text.Json.Serialization;
namespace D2L.Bmx;

public struct AuthenticateChallengeMfaOptions {
	public string FactorId { get; set; }
	public string PassCode { get; set; }
	public string StateToken { get; set; }

	public AuthenticateChallengeMfaOptions( string factorId, string passCode, string stateToken ) {
		FactorId = factorId;
		PassCode = passCode;
		StateToken = stateToken;
	}
}

public struct AuthenticateOptions {
	public string Username { get; set; }
	public string Password { get; set; }

	public AuthenticateOptions( string username, string password ) {
		Username = username;
		Password = password;
	}
}

public class AuthenticateResponse {
	public DateTimeOffset ExpiresAt { get; set; }

	[JsonConverter( typeof( JsonStringEnumConverter ) )]
	public AuthenticationStatus Status { get; set; }
}

public class AuthenticateResponseInital : AuthenticateResponse {
	public string StateToken { get; set; }
	[JsonPropertyName( "_embedded" )] public AuthenticateResponseEmbeddedInitial Embedded { get; set; }
}

public class AuthenticateResponseSuccess : AuthenticateResponse {
	public string SessionToken { get; set; }
}

public struct AuthenticateResponseEmbeddedInitial {
	public OktaMfaFactor[] Factors { get; set; }
}

public enum AuthenticationStatus {
	UNKNOWN,
	PASSWORD_WARN,
	SUCCESS,
	LOCKED_OUT,
	MFA_REQUIRED,
	MFA_CHALLENGE,
}

public struct OktaApp {
	public string Id { get; set; }
	public string Label { get; set; }
	public string AppName { get; set; }
	public string LinkUrl { get; set; }
}

public struct OktaError {
	public string ErrorCode { get; set; }
	public string ErrorSumamry { get; set; }
	public string ErrorLink { get; set; }
	public string ErrorId { get; set; }
}

public struct OktaMfaFactor {
	public string Id { get; set; }
	public string FactorType { get; set; }
	public string Provider { get; set; }
	public string VendorName { get; set; }
}

public struct OktaSession {
	public string Id { get; set; }
	public string UserId { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset ExpiresAt { get; set; }
}

public struct SessionOptions {
	public string SessionToken { get; set; }

	public SessionOptions( string sessionToken ) {
		SessionToken = sessionToken;
	}
}
