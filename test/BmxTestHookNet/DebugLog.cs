using System.Runtime.InteropServices;

namespace BmxTestHookNet;

internal static unsafe class DebugLog {
	private static bool s_enabled;
	private static nint s_stderrHandle;
	private static nint s_originalWriteFile;

	public static void Init( bool enabled ) {
		s_enabled = enabled;

		// Cache the real stderr handle and WriteFile address before any hooks
		s_stderrHandle = NativeMethods.GetStdHandle( NativeMethods.STD_ERROR_HANDLE );

		nint kernel32 = NativeMethods.GetModuleHandleW( "kernel32.dll" );
		if( kernel32 != 0 ) {
			s_originalWriteFile = NativeMethods.GetProcAddress( kernel32, "WriteFile" );
		}
	}

	public static bool IsEnabled => s_enabled;

	public static void Log( string message ) {
		if( !s_enabled ) return;

		string line = $"[BmxTestHookNet] {message}\r\n";

		if( s_originalWriteFile != 0 && s_stderrHandle != 0 ) {
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes( line );
			fixed( byte* pBytes = bytes ) {
				uint written;
				var fn = (delegate* unmanaged[Stdcall]<nint, byte*, uint, uint*, nint, int>)s_originalWriteFile;
				fn( s_stderrHandle, pBytes, (uint)bytes.Length, &written, 0 );
			}
		}
	}
}
