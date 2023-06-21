using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;


namespace D2L.Bmx;

internal class MacParentProcess {

	[DllImport( "libproc.dylib", CharSet = CharSet.Ansi, SetLastError = true )]
	public static extern int proc_pidinfo( int pid, int flavor, uint arg, ref PROC_PPIDINFO buffer, int buffersize );

	[DllImport( "libproc.dylib", CharSet = CharSet.Ansi, SetLastError = true )]
	public static extern int proc_name( int pid, StringBuilder buf, uint buffersize );

	[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Ansi )]
	public struct PROC_PPIDINFO {
		public int ppi_pid;
		public int ppi_ppid;
	}

	public static string GetParentProcessName() {
		int myPid = Process.GetCurrentProcess().Id;
		var ppi = new PROC_PPIDINFO();

		int ret = proc_pidinfo( myPid, 4 /* PROC_PIDT_PPIDINFO */, 0, ref ppi, Marshal.SizeOf( ppi ) );
		if( ret <= 0 ) {
			Console.WriteLine( "Error calling proc_pidinfo" );
			return "";
		}

		var procName = new StringBuilder( 1024 );
		proc_name( ppi.ppi_ppid, procName, (uint)procName.Capacity );
		return procName.ToString();
	}
}
