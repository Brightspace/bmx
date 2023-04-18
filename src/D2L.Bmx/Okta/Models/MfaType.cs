namespace D2L.Bmx.Okta.Models;

internal record MfaOption(
	string Name,
	string Provider,
	MfaType Type
);

// TODO: More robust / generic way of handling MFA Types
internal enum MfaType {
	Challenge, // Ex: totp or yubikey types
	Question,
	Sms,
	Unknown // Okta for example supports more MFA types that BMX might be unable to handle, class this as unknown
}
