namespace D2L.Bmx;

internal static class BmxPaths {
	public static readonly string USER_HOME_DIR = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
	public static readonly string BMX_DIR = Path.Join( USER_HOME_DIR, ".bmx" );
	public static readonly string SESSIONS_FILE_NAME = Path.Join( BMX_DIR, "sessions" );
	public static readonly string CONFIG_FILE_NAME = Path.Join( BMX_DIR, "config" );
	public static readonly string AWS_CREDS_CACHE_FILE_NAME = Path.Join( BMX_DIR, "awsCredsCache" );
}
