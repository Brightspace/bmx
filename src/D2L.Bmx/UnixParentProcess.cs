using System.Diagnostics;

namespace D2L.Bmx;

class UnixParentProcess {

	public static string GetParentProcessName() {
		var parentPid = GetParentProcessPid();
		Console.WriteLine( $"pid is {parentPid}" );
		var xParentPid = GetXParentProcessPid( parentPid );
		Console.WriteLine( $"{xParentPid}" );
		Process proc = new Process {
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
	private static string GetXParentProcessPid( string pid ) {
		Process proc = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = "/bin/bash",
				Arguments = $"-c \"ps -p {pid} -o ppid=\"",
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

	private static string GetParentProcessPid() {
		Process proc = new Process {
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
