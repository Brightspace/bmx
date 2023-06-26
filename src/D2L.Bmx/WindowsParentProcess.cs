using System.Diagnostics;
using System.Runtime.InteropServices;

namespace D2L.Bmx;

internal partial class WindowsParentProcess {

	[DllImport( "ntdll.dll", EntryPoint = "NtQueryInformationProcess" )]
	internal static extern int NtQueryInformationProcess(
		IntPtr processHandle,
		int processInformationClass,
		ref PROCESS_BASIC_INFORMATION processInformation,
		int processInformationLength,
		out int returnLength
	);

	internal struct PROCESS_BASIC_INFORMATION {
		public IntPtr ExitStatus;
		public IntPtr PebBaseAddress;
		public IntPtr AffinityMask;
		public IntPtr BasePriority;
		public UIntPtr UniqueProcessId;
		public UIntPtr InheritedFromUniqueProcessId;
	}

	private static int GetParentProcessId() {
		var proc = Process.GetCurrentProcess();
		var pbi = new PROCESS_BASIC_INFORMATION();
		int status = NtQueryInformationProcess(
			proc.Handle,
			0,
			ref pbi,
			Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(),
			out int infoLen
		);

		if( status != 0 ) {
			return -1;
		}
		return (int)pbi.InheritedFromUniqueProcessId;
	}

	public static string GetParentProcessName() {
		return Process.GetProcessById( GetParentProcessId() ).ProcessName;
	}
}
