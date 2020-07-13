using Bmx.Core;
using Bmx.Idp.Okta;
using Bmx.Service.Aws;
using Microsoft.Extensions.DependencyInjection;

namespace Bmx.CommandLine {
	class Program {
		static void Main( string[] args ) {
			var services = ConfigureServices().BuildServiceProvider();
			var cmdLine = new CommandLine( services.GetRequiredService<IBmxCore>() );
			cmdLine.InvokeAsync( args ).Wait();
		}


		private static IServiceCollection ConfigureServices() {
			IServiceCollection services = new ServiceCollection();
			// TODO: Move state out to BmxCore and make these two transient
			services.AddSingleton<IIdentityProvider, OktaClient>();
			services.AddSingleton<ICloudProvider, AwsClient>();

			services.AddSingleton<IBmxCore, BmxCore>();
			return services;
		}
	}
}
