using Amazon.SecurityToken;
using Bmx.Core;
using Bmx.Idp.Okta;
using Bmx.Service.Aws;
using Microsoft.Extensions.DependencyInjection;

namespace Bmx.CommandLine {
	class Program {
		static void Main( string[] args ) {
			var config = new IniConfiguration();
			var services = ConfigureServices().BuildServiceProvider();
			var cmdLine = new CommandLine( services.GetRequiredService<IBmxCore>() );
			cmdLine.InvokeAsync( args ).Wait();
		}


		private static IServiceCollection ConfigureServices() {
			IServiceCollection services = new ServiceCollection();

			services.AddSingleton<IOktaApi, OktaApi>();
			services.AddSingleton<IAmazonSecurityTokenService, AmazonSecurityTokenServiceClient>();

			// TODO: Move state out to BmxCore and make these two transient
			services.AddSingleton<IIdentityProvider, OktaClient>();
			services.AddSingleton<ICloudProvider, AwsClient>();

			services.AddSingleton<IBmxCore, BmxCore>();
			return services;
		}
	}
}
