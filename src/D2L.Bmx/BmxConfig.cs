namespace D2L.Bmx;

internal static class PasswordlessTimeoutDefaults {
	public const int Min = 5;
	public const int Max = 30;
	public const int Default = 60;
}

internal record BmxConfig(
	string? Org,
	string? User,
	string? Account,
	string? Role,
	string? Profile,
	int? Duration,
	int? PasswordlessTimeout
);
