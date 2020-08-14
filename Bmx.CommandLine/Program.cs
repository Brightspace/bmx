using Amazon.SecurityToken;
using Bmx.Core;
using Bmx.Idp.Okta;
using Bmx.Idp.Okta.State;
using Bmx.Service.Aws;
using Bmx.Service.Aws.State;
using Microsoft.Extensions.DependencyInjection;

namespace Bmx.CommandLine {
	class Program {
		static void Main( string[] args ) {
			var config = new IniConfiguration();
			var services = ConfigureServices().BuildServiceProvider();
			var cmdLine =
				new CommandLine<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState, AwsRoleState>(
					services
						.GetRequiredService<IBmxCore<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState,
							AwsRoleState>>(), config );
			cmdLine.InvokeAsync( args ).Wait();
		}


		private static IServiceCollection ConfigureServices() {
			IServiceCollection services = new ServiceCollection();

			services.AddSingleton<IOktaApi, OktaApi>();
			services.AddSingleton<IAmazonSecurityTokenService, AmazonSecurityTokenServiceClient>();

			// TODO: Move state out to BmxCore and make these two transient
			services
				.AddSingleton<IIdentityProvider<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState>,
					OktaClient>();
			services.AddSingleton<ICloudProvider<AwsRoleState>, AwsClient>();

			services
				.AddSingleton<IBmxCore<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState, AwsRoleState>,
					BmxCore<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState, AwsRoleState>>();
			return services;
		}
	}
}
