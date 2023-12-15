namespace D2L.Bmx;

internal static class BmxPaths {
	private static readonly string BMX_DIR = Path.Join(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".bmx" );

	public static readonly string CACHE_DIR = Path.Join( BMX_DIR, "cache" );
	public static readonly string CONFIG_FILE_NAME = Path.Join( BMX_DIR, "config" );
	public static readonly string SESSIONS_FILE_NAME = Path.Join( CACHE_DIR, "sessions" );
	public static readonly string SESSIONS_FILE_LEGACY_NAME = Path.Join( BMX_DIR, "sessions" );
	public static readonly string UPDATE_CHECK_FILE_NAME = Path.Join( CACHE_DIR, "updateCheck" );
	public static readonly string AWS_CREDS_CACHE_FILE_NAME = Path.Join( CACHE_DIR, "awsCreds" );
	public static readonly string OLD_BMX_VERSION_FILE_NAME = Path.Join( BMX_DIR, "bmx" );
}
