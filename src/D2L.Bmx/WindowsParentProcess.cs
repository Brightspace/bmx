using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace D2L.Bmx;

internal class WindowsParentProcess {

	[DllImport( "kernel32.dll", SetLastError = true )]
	private static extern IntPtr CreateToolhelp32Snapshot( uint dwFlags, uint th32ProcId );

	[DllImport( "kernel32.dll" )]
	private static extern bool Process32First( IntPtr hSnapshot, ref PROCESSENTRY32 lppe );

	[DllImport( "kernel32.dll" )]
	private static extern bool Process32Next( IntPtr hSnapshot, ref PROCESSENTRY32 lppe );

	[StructLayout( LayoutKind.Sequential )]
	private struct PROCESSENTRY32 {
		public uint dwSize;
		public uint cntUsage;
		public uint th32ProcessId;
		public IntPtr th32DefaultHeapID;
		public uint th32ModuleId;
		public uint cntThreads;
		public uint th32ParentProcessID;
		public int pcPriClassBase;
		public uint dwFlags;
		[MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
		public string szExeFile;
	}

	private const uint TH32CS_SNAPPROCESS = 2;

	[RequiresDynamicCode( "Calls System.Runtime.InteropServices.Marshal.SizeOf(Type)" )]
	private static int GetParentProcessId() {
		int bmxProcId = Process.GetCurrentProcess().Id;
		IntPtr snapshot = CreateToolhelp32Snapshot( TH32CS_SNAPPROCESS, 0 );
		var procInfo = new PROCESSENTRY32();
		procInfo.dwSize = (uint)Marshal.SizeOf( typeof( PROCESSENTRY32 ) );

		if( Process32First( snapshot, ref procInfo ) == false ) {
			return -1;
		}

		do {
			if( bmxProcId == procInfo.th32ProcessId ) {
				return (int)procInfo.th32ParentProcessID;
			}
		} while( Process32Next( snapshot, ref procInfo ) );
		return -1;
	}

	public static string GetParentProcessName() {
		return Process.GetProcessById( GetParentProcessId() ).ProcessName;
	}
}
