using System.Runtime.InteropServices;

namespace D2L.Bmx;

// The code here exists because legacy Windows console doesn't support ANSI escape codes by default.
// We can consider deleting everything here once Windows 10 is out of support and
// Windows Terminal becomes the default console on all supported Windows versions.
internal static partial class VirtualTerminal {
	private const string Kernel32 = "kernel32.dll";
	private const int STD_ERROR_HANDLE = -12;
	private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

	private static bool _enablingAttempted = false;
	private static bool _enabled = false;

	// https://learn.microsoft.com/en-us/windows/console/classic-vs-vt
	public static bool TryEnableOnStderr() {
		if( _enablingAttempted ) {
			return _enabled;
		}
		_enablingAttempted = true;

		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			// POSIX systems all support ANSI escape codes
			_enabled = true;
			return true;
		}
		nint stderrHandle = GetStdHandle( STD_ERROR_HANDLE );
		if( !GetConsoleMode( stderrHandle, out int mode ) ) {
			_enabled = false;
			return false;
		}
		if( ( mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING ) == ENABLE_VIRTUAL_TERMINAL_PROCESSING ) {
			_enabled = true;
			return true;
		}
		_enabled = SetConsoleMode( stderrHandle, (int)( mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING ) );
		return _enabled;
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
