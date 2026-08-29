using System.Runtime.InteropServices;

namespace BmxTestHookNet;

internal static partial class NativeMethods {

	[LibraryImport( "kernel32.dll", SetLastError = true )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static partial bool VirtualProtect(
		nint lpAddress,
		nuint dwSize,
		uint flNewProtect,
		out uint lpflOldProtect
	);

	public const uint PAGE_READWRITE = 0x04;

	[LibraryImport( "kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16 )]
	public static partial nint GetModuleHandleW( string? lpModuleName );

	[LibraryImport( "kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf8 )]
	public static partial nint GetProcAddress( nint hModule, string lpProcName );

	[LibraryImport( "kernel32.dll", SetLastError = true )]
	public static partial nint GetStdHandle( int nStdHandle );

	public const int STD_INPUT_HANDLE = -10;
	public const int STD_OUTPUT_HANDLE = -11;
	public const int STD_ERROR_HANDLE = -12;

	[DllImport( "kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern unsafe bool WriteConsoleW(
		nint hConsoleOutput,
		char* lpBuffer,
		uint nNumberOfCharsToWrite,
		uint* lpNumberOfCharsWritten,
		nint lpReserved
	);

	[DllImport( "kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern unsafe bool ReadConsoleW(
		nint hConsoleInput,
		char* lpBuffer,
		uint nNumberOfCharsToRead,
		uint* lpNumberOfCharsRead,
		nint pInputControl
	);

	[DllImport( "kernel32.dll", SetLastError = true )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern unsafe bool ReadConsoleInputW(
		nint hConsoleInput,
		INPUT_RECORD* lpBuffer,
		uint nLength,
		uint* lpNumberOfEventsRead
	);

	[StructLayout( LayoutKind.Sequential )]
	public struct IMAGE_DOS_HEADER {
		public ushort e_magic;
		// 29 words of other fields we skip
		public ushort e_cblp, e_cp, e_crlc, e_cparhdr, e_minalloc, e_maxalloc;
		public ushort e_ss, e_sp, e_csum, e_ip, e_cs, e_lfarlc, e_ovno;
		public unsafe fixed ushort e_res[4];
		public ushort e_oemid, e_oeminfo;
		public unsafe fixed ushort e_res2[10];
		public int e_lfanew;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct IMAGE_IMPORT_DESCRIPTOR {
		public uint OriginalFirstThunk; // RVA to INT (Import Name Table)
		public uint TimeDateStamp;
		public uint ForwarderChain;
		public uint Name;               // RVA to DLL name string
		public uint FirstThunk;         // RVA to IAT (Import Address Table)
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct KEY_EVENT_RECORD {
		public int bKeyDown;
		public ushort wRepeatCount;
		public ushort wVirtualKeyCode;
		public ushort wVirtualScanCode;
		public char UnicodeChar;
		public uint dwControlKeyState;
	}

	[StructLayout( LayoutKind.Explicit )]
	public struct INPUT_RECORD {
		[FieldOffset( 0 )]
		public ushort EventType;
		[FieldOffset( 4 )]
		public KEY_EVENT_RECORD KeyEvent;
	}

	public const ushort KEY_EVENT = 0x0001;
	public const ushort VK_RETURN = 0x0D;
}
