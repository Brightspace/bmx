using System.Diagnostics;
using System.Runtime.InteropServices;

namespace D2L.Bmx;

/**
This file may eventually be replaced if the following dotnet runtime api proposals are implemented:
- https://github.com/dotnet/runtime/issues/21941
- https://github.com/dotnet/runtime/issues/24423
*/
internal partial class ParentProcess {
	public static string? GetParentProcessName() {
		int parentPid = -1;
		if( OperatingSystem.IsWindows() ) {
			parentPid = GetWindowsParentProcessId();
		} else if( OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ) {
			parentPid = GetPosixParentProcessId();
		}
		if( parentPid == -1 ) {
			return null;
		}
		return Process.GetProcessById( parentPid ).ProcessName;
	}

	[LibraryImport( "libc", EntryPoint = "getppid" )]
	private static partial int GetPosixParentProcessId();


	// Uses the same approach of calling NtQueryInformationProcess as in the PowerShell library
	// https://github.com/PowerShell/PowerShell/blob/26f621952910e33840efb0c539fbef1e2a467a0d/src/System.Management.Automation/engine/ProcessCodeMethods.cs
	private static int GetWindowsParentProcessId() {
		var proc = Process.GetCurrentProcess();
		var pbi = new PROCESS_BASIC_INFORMATION();
		int status = NtQueryInformationProcess(
			proc.Handle,
			0,
			ref pbi,
			Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(),
			out _
		);

		if( status != 0 ) {
			return -1;
		}
		return (int)pbi.InheritedFromUniqueProcessId;
	}

	[LibraryImport( "ntdll.dll" )]
	private static partial int NtQueryInformationProcess(
		IntPtr processHandle,
		int processInformationClass,
		ref PROCESS_BASIC_INFORMATION processInformation,
		int processInformationLength,
		out int returnLength
	);

	private struct PROCESS_BASIC_INFORMATION {
		public IntPtr ExitStatus;
		public IntPtr PebBaseAddress;
		public IntPtr AffinityMask;
		public IntPtr BasePriority;
		public UIntPtr UniqueProcessId;
		public UIntPtr InheritedFromUniqueProcessId;
	}
}
