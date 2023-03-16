namespace D2L.Bmx.Okta.Models;

internal record OktaMfaFactor(
	string Id,
	string FactorType,
	string Provider,
	string VendorName
);
