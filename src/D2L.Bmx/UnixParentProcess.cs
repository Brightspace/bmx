using System.Diagnostics;

namespace D2L.Bmx;

internal class UnixParentProcess {

	public static string GetParentName() {
		string parentPid = GetParentPid();

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

	private static string GetParentPid() {
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
