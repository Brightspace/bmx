namespace Bmx.CommandLine {
	class Program {
		static void Main( string[] args ) {
			var cmdLine = new CommandLine();
			cmdLine.InvokeAsync( args ).Wait();
		}
	}
}
