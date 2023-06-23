using System.Diagnostics;

namespace D2L.Bmx;

internal class UnixParentProcess {

	public static string GetParentProcessName() {
		string parentPid = GetParentProcessPid();
		string xParentPid = GetParentProcessPid( parentPid );
		var proc = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = $"-c \"ps -p {xParentPid} -o comm=\"",
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
	private static string GetParentProcessPid( string pid ) {
		var proc = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = $"-c \"ps -p {pid} -o ppid=\"",
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

	private static string GetParentProcessPid() {
		var proc = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = "-c \"ps -p $$ -o ppid=\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			}
		};

		proc.Start();
		string parentPid = proc.StandardOutput.ReadToEnd().Trim();
		proc.WaitForExit();
		return parentPid;
	}
}