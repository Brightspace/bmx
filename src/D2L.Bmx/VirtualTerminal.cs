using System.Runtime.InteropServices;

namespace D2L.Bmx;

// The code here exists because legacy Windows console doesn't support ANSI escape codes by default.
// We can consider deleting everything here once Windows 10 is out of support and
// Windows Terminal becomes the default console on all supported Windows versions.
internal static partial class VirtualTerminal {
	private const string Kernel32 = "kernel32.dll";
	private const int STD_ERROR_HANDLE = -12;
	private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

	// https://learn.microsoft.com/en-us/windows/console/classic-vs-vt
	public static bool TryEnableOnStderr() {
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			// POSIX systems all support ANSI escape codes
			return true;
		}
		if( Environment.GetEnvironmentVariable( "TERM_PROGRAM" ) == "mintty" ) {
			// mintty supports ANSI escape codes, but console-related Win32 API calls will fail in it,
			// so we must handle it specially and before making any Win32 API calls.
			return true;
		}
		nint stderrHandle = GetStdHandle( STD_ERROR_HANDLE );
		if( !GetConsoleMode( stderrHandle, out int mode ) ) {
			return false;
		}
		if( ( mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING ) == ENABLE_VIRTUAL_TERMINAL_PROCESSING ) {
			return true;
		}
		return SetConsoleMode( stderrHandle, (int)( mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING ) );
	}

	// https://learn.microsoft.com/en-us/windows/console/getstdhandle
	[LibraryImport( Kernel32 )]
	private static partial IntPtr GetStdHandle( int nStdHandle );

	// https://learn.microsoft.com/en-us/windows/console/getconsolemode
	[LibraryImport( Kernel32 )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static partial bool GetConsoleMode( IntPtr handle, out int mode );

	// https://learn.microsoft.com/en-us/windows/console/setconsolemode
	[LibraryImport( Kernel32 )]
	[return: MarshalAs( UnmanagedType.Bool )]
	private static partial bool SetConsoleMode( IntPtr handle, int mode );
}
