namespace D2L.Bmx.Okta.Models;

internal record OktaError(
	string ErrorCode,
	string ErrorSummary,
	string ErrorLink,
	string ErrorId
);
