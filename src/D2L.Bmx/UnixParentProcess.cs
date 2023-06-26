using System.Diagnostics;

namespace D2L.Bmx;

internal class UnixParentProcess {

	public static string GetParentProcessName() {
		int parentPid = GetParentProcessId( GetParentProcessId( Process.GetCurrentProcess().Id ) );
		return Process.GetProcessById( parentPid ).ProcessName;
	}

	private static int GetParentProcessId( int pid ) {
		var proccessStartInfo = new ProcessStartInfo {
			FileName = "ps",
			ArgumentList = { "-p", $"{pid}", "-o", "ppid=" },
			RedirectStandardOutput = true,
		};

		using var proc = Process.Start( proccessStartInfo );
		if( proc is null ) {
			return -1;
		}

		string output = proc.StandardOutput.ReadToEnd();
		proc.WaitForExit();
		if( int.TryParse( output.Trim(), out int parentPid ) ) {
			return parentPid;
		}
		return -1;
	}
}
