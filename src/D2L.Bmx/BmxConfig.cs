namespace D2L.Bmx;

internal record BmxConfig(
	string? Org,
	string? User,
	string? Account,
	string? Role,
	string? Profile,
	int? DefaultDuration,
	string? defaultMfa
);
