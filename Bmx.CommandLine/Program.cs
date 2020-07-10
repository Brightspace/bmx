using Bmx.Core;
using Bmx.Idp.Okta;
using Bmx.Service.Aws;

namespace Bmx.CommandLine {
	class Program {
		// TODO: Try https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1
		static void Main( string[] args ) {
			var okta = new OktaClient();
			var aws = new AwsClient();
			var bmx = new BmxCore(okta, aws);
			var cmdLine = new CommandLine(bmx);
			cmdLine.InvokeAsync( args ).Wait();
		}
	}
}
