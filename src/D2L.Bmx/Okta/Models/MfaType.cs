namespace D2L.Bmx.Okta.Models;

internal record MfaOption(
	string Name,
	MfaType Type
);

// TODO: More robust / generic way of handling MFA Types
internal enum MfaType {
	Challenge, // Ex: totp
	Verify, // Ex: Push to verify
	Unknown // Okta for example supports more MFA types that BMX might be unable to handle, class this as unknown
}
