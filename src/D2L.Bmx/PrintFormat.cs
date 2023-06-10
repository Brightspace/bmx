namespace D2L.Bmx;

internal static class PrintFormat {
	public static readonly HashSet<string> All = new( StringComparer.OrdinalIgnoreCase ) {
		Bash,
		PowerShell,
		Json,
	};
	public const string Bash = "Bash";
	public const string PowerShell = "PowerShell";
	public const string Json = "JSON";
}
