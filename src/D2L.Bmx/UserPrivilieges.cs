using System.Runtime.InteropServices;
using System.Security.Principal;

namespace D2L.Bmx;

internal static partial class UserPrivileges {

	[LibraryImport( "libc", EntryPoint = "geteuid" )]
	internal static partial uint GetPosixEuid();

	internal static bool HasElevatedPermissions() {
		bool isElevated = false;
		if( OperatingSystem.IsWindows() ) {
			isElevated = new WindowsPrincipal( WindowsIdentity.GetCurrent() ).IsInRole( WindowsBuiltInRole.Administrator );
		} else if( OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ) {
			isElevated = GetPosixEuid() == 0;
		}
		return isElevated;
	}
}
