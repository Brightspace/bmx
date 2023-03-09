using System.Text.Json.Serialization;
namespace D2L.Bmx;
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
