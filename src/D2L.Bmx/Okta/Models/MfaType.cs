namespace D2L.Bmx.Okta.Models;

public struct MfaOption {
	public string Name { get; set; }
	public MfaType Type { get; set; }

	public MfaOption( string name, MfaType type ) {
		Name = name;
		Type = type;
	}
}

// TODO: More robust / generic way of handling MFA Types
public enum MfaType {
	Challenge, // Ex: totp
	Verify, // Ex: Push to verify
	Unknown // Okta for example supports more MFA types that BMX might be unable to handle, class this as unknown
}
