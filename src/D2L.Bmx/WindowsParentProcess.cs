using System.Diagnostics;
using System.Runtime.InteropServices;

namespace D2L.Bmx;
/**
This file may eventually be replaced if the following dotnet runtime api proposals are implemented:
- https://github.com/dotnet/runtime/issues/21941
- https://github.com/dotnet/runtime/issues/24423

Uses the same approach of calling NtQueryInformationProcess as in the PowerShell library
https://github.com/PowerShell/PowerShell/blob/26f621952910e33840efb0c539fbef1e2a467a0d/src/System.Management.Automation/engine/ProcessCodeMethods.cs
*/
internal partial class WindowsParentProcess {

	[LibraryImport( "ntdll.dll", EntryPoint = "NtQueryInformationProcess" )]
	internal static partial int NtQueryInformationProcess(
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
