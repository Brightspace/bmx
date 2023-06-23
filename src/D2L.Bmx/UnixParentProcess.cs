using System.Diagnostics;

namespace D2L.Bmx;

internal class UnixParentProcess {

	public static string GetParentProcessName() {
		string parentPid = GetParentProcessPid( GetParentProcessPid() );
		var proc = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = $"-c \"ps -p {parentPid} -o comm=\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			}
		};

		proc.Start();
		string parentProcName = proc.StandardOutput.ReadToEnd().Trim();
		proc.WaitForExit();
		return parentProcName;
	}
	private static string GetParentProcessPid( string? pid = null ) {
		var proc = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = String.IsNullOrEmpty( pid ) ? "-c \"ps -p $$ -o ppid=\"" : $"-c \"ps -p {pid} -o ppid=\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			}
		};

		proc.Start();
		string parentPid = proc.StandardOutput.ReadToEnd().Trim().Replace( "-", "" );
		proc.WaitForExit();
		return parentPid;
	}
}
