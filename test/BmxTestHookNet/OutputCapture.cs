namespace BmxTestHookNet;

internal static class OutputCapture {
	private static StreamWriter? s_stdoutWriter;
	private static StreamWriter? s_stderrWriter;
	private static nint s_stdoutHandle;
	private static nint s_stderrHandle;
	private static readonly Lock s_lock = new();

	public static void Init() {
		s_stdoutHandle = NativeMethods.GetStdHandle( NativeMethods.STD_OUTPUT_HANDLE );
		s_stderrHandle = NativeMethods.GetStdHandle( NativeMethods.STD_ERROR_HANDLE );

		string? stdoutFile = Environment.GetEnvironmentVariable( "BMX_TEST_STDOUT_FILE" );
		string? stderrFile = Environment.GetEnvironmentVariable( "BMX_TEST_STDERR_FILE" );

		if( !string.IsNullOrEmpty( stdoutFile ) ) {
			s_stdoutWriter = new StreamWriter( stdoutFile, append: false ) { AutoFlush = true };
			DebugLog.Log( $"Capturing stdout to: {stdoutFile}" );
		}

		if( !string.IsNullOrEmpty( stderrFile ) ) {
			s_stderrWriter = new StreamWriter( stderrFile, append: false ) { AutoFlush = true };
			DebugLog.Log( $"Capturing stderr to: {stderrFile}" );
		}
	}

	public static void OnWriteText( nint consoleHandle, string text ) {
		lock( s_lock ) {
			if( consoleHandle == s_stdoutHandle ) {
				s_stdoutWriter?.Write( text );
			} else if( consoleHandle == s_stderrHandle ) {
				s_stderrWriter?.Write( text );
			} else {
				// Unknown handle so write to both just in case
				s_stdoutWriter?.Write( text );
			}
		}
	}

	public static void Shutdown() {
		lock( s_lock ) {
			s_stdoutWriter?.Flush();
			s_stdoutWriter?.Dispose();
			s_stdoutWriter = null;

			s_stderrWriter?.Flush();
			s_stderrWriter?.Dispose();
			s_stderrWriter = null;
		}

		DebugLog.Log( "Output capture shut down" );
	}
}
