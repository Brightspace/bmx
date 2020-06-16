namespace Bmx.CommandLine {
	class Program {
		// TODO: Try https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1
		static void Main( string[] args ) {
			var cmdLine = new CommandLine();
			cmdLine.InvokeAsync( args ).Wait();
		}
	}
}
