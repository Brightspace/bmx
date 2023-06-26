using System.Diagnostics;

namespace D2L.Bmx;

internal class UnixParentProcess {

	public static string GetParentProcessName() {
		int parentPid = GetParentProcessId( GetParentProcessId( Process.GetCurrentProcess().Id ) );
		var proccessStartInfo = new ProcessStartInfo {
			FileName = "/bin/ps",
			ArgumentList = { "-p", $"{parentPid}", "-o", "comm=" },
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using( var proc = Process.Start( proccessStartInfo ) ) {
			if( proc is not null ) {
				string output = proc.StandardOutput.ReadToEnd().Replace( "-", "" );
				proc.WaitForExit();
				return output;
			}
			return "bash";
		}
	}

	private static int GetParentProcessId( int pid ) {
		var proccessStartInfo = new ProcessStartInfo {
			FileName = "/bin/ps",
			ArgumentList = { "-p", $"{pid}", "-o", "ppid=" },
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using( var proc = Process.Start( proccessStartInfo ) ) {
			if( proc is not null ) {
				string output = proc.StandardOutput.ReadToEnd();
				proc.WaitForExit();
				if( int.TryParse( output.Trim(), out int parentPid ) ) {
					return parentPid;
				}
			}
			return -1;
		}
	}
}