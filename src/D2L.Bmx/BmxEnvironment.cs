namespace D2L.Bmx;

internal static class BmxEnvironment {
	public static bool IsDebug { get; } = Environment.GetEnvironmentVariable( "BMX_DEBUG" ) == "1";
}
