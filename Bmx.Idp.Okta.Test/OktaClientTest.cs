using System;
using System.Threading.Tasks;
using Bmx.Core;
using Bmx.Idp.Okta.Models;
using Bmx.Idp.Okta.State;
using Moq;
using NUnit.Framework;

namespace Bmx.Idp.Okta.Test {
	[Category( "Okta" )]
	public class OktaClientTests {
		private Mock<IOktaApi> _mockOktaApi;
		private IIdentityProvider<OktaAuthenticateState, OktaAuthenticatedState, OktaAccountState> _oktaClient;

		[SetUp]
		public void Setup() {
			_mockOktaApi = new Mock<IOktaApi>();
			_oktaClient = new OktaClient( _mockOktaApi.Object );
		}

		[Test]
		[Category( "Authenticate" )]
		public async Task AuthenticateMfaRequiredHasMfaOptions() {
			_mockOktaApi.Setup( api => api.AuthenticateOkta( It.IsAny<AuthenticateOptions>() ) )
				.ReturnsAsync( ApiResponsesExamples.AuthnResponseInitialSuccess );

			var response = await _oktaClient.Authenticate( "", "" );
			Assert.AreEqual(
				new[] {
					new MfaOption {Name = "token:software:totp", Type = MfaType.Challenge},
					new MfaOption {Name = "token:software:totp", Type = MfaType.Challenge}
				}, response.MfaOptions );
		}

		[Test]
		[Category( "GetAccounts" )]
		public async Task GetAccountsHasAccounts() {
			_mockOktaApi.Setup( api => api.CreateSessionOkta( It.IsAny<SessionOptions>() ) )
				.ReturnsAsync( new OktaSession {Id = "123"} );
			_mockOktaApi.Setup( api => api.AddSession( It.IsAny<string>() ) );
			_mockOktaApi.Setup( api => api.GetAccountsOkta( It.IsAny<string>() ) )
				.ReturnsAsync( ApiResponsesExamples.OktaAppSuccess );

			var state = await _oktaClient.GetAccounts( new OktaAuthenticatedState( true, "123" ), "amazon_aws" );

			Assert.AreEqual( new[] {"Dev-Foo"}, state.Accounts );
		}

		[Test]
		[Category( "GetServiceProviderSaml" )]
		public async Task GetServiceProviderSamlExtract() {
			_mockOktaApi.Setup( api => api.GetAccountOkta( It.IsAny<Uri>() ) )
				.ReturnsAsync( ApiResponsesExamples.OktaAppLinkSuccessHtml );
			string actual = await _oktaClient.GetServiceProviderSaml(
				new OktaAccountState(
					new OktaApp[] {
						new OktaApp() {
							Id = "foo", AppName = "foo", LinkUrl = "http://localhost:8080/foo", Label = "foo"
						}
					}, "amazon_aws" ), 0 );
			Assert.AreEqual( ApiResponsesExamples.OktaApiApplinkSamlString, actual );
		}
	}
}
