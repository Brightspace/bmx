namespace D2L.Bmx.Okta.Models;

internal record OktaError(
	string ErrorCode,
	string ErrorSumamry,
	string ErrorLink,
	string ErrorId
);
