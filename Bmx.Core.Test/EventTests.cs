using System;
using System.Collections.Generic;
using System.Linq;
using Bmx.Core.State;
using Bmx.Core.Test.State;
using Moq;
using NUnit.Framework;

namespace Bmx.Core.Test {
	[TestFixture]
	[Category( "Events" )]
	public class EventTests {
		private Mock<IIdentityProvider<IAuthenticateState, IAuthenticatedState, IAccountState>>
			_mockIdentityProvider;

		private Mock<ICloudProvider<IRoleState>> _mockCloudProvider;

		private IBmxCore<IAuthenticateState, IAuthenticatedState, IAccountState, IRoleState> _bmxCore;

		private const string MockIdentityProviderName = "mockProvider";

		[SetUp]
		public void Setup() {
			_mockIdentityProvider =
				new Mock<IIdentityProvider<IAuthenticateState, IAuthenticatedState, IAccountState>>();
			_mockCloudProvider = new Mock<ICloudProvider<IRoleState>>();

			_bmxCore = new BmxCore<IAuthenticateState, IAuthenticatedState, IAccountState, IRoleState>(
				_mockIdentityProvider.Object, _mockCloudProvider.Object );

			_bmxCore.PromptUserName += provider => "";
			_bmxCore.PromptUserPassword += provider => "";
			_bmxCore.PromptMfaType += options => 0;
			_bmxCore.PromptMfaInput += prompt => "";
			_bmxCore.PromptAccountSelection += accounts => 0;
			_bmxCore.PromptRoleSelection += roles => 0;

			_mockIdentityProvider.Setup( m => m.Name ).Returns( MockIdentityProviderName );
			_mockIdentityProvider.Setup( provider => provider.Authenticate( It.IsAny<string>(), It.IsAny<string>() ) )
				.ReturnsAsync( new MockAuthenticateState() );
			_mockIdentityProvider
				.Setup( provider =>
					provider.ChallengeMfa( It.IsAny<MockAuthenticateState>(), It.IsAny<int>(), It.IsAny<string>() ) )
				.ReturnsAsync( new MockAuthenticatedState() );
			_mockIdentityProvider
				.Setup( provider => provider.GetAccounts( It.IsAny<MockAuthenticatedState>(), It.IsAny<string>() ) )
				.ReturnsAsync( new MockAccountState() );
			_mockIdentityProvider
				.Setup( provider => provider.GetServiceProviderSaml( It.IsAny<MockAccountState>(), It.IsAny<int>() ) )
				.ReturnsAsync( "" );

			_mockCloudProvider.Setup( provider => provider.GetRoles( It.IsAny<string>() ) )
				.Returns( new MockRoleState() );
			_mockCloudProvider.Setup( provider => provider.GetTokens( It.IsAny<MockRoleState>(), It.IsAny<int>() ) )
				.ReturnsAsync( new Dictionary<string, string>() );
		}

		/*
		 * Tests if BMXCore fires input prompt events if it didn't receive values for required input at the start
		 * (Inputs can either be at the start or interactively via prompts)
		 */
		[Test]
		[Category( "PromptUsername" )]
		public void PromptUserNameCalled() {
			bool eventRaised = false;
			_bmxCore.PromptUserName += providerName => {
				eventRaised = true;
				return "";
			};
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "PromptUsername" )]
		public void PromptUserNameProviderName() {
			string idpName = null;
			_bmxCore.PromptUserName += providerName => {
				idpName = providerName;
				return "";
			};
			_bmxCore.Print( "org" );
			Assert.AreEqual( MockIdentityProviderName, idpName );
		}

		[Test]
		[Category( "PromptPassword" )]
		public void PromptUserPasswordCalled() {
			bool eventRaised = false;
			_bmxCore.PromptUserPassword += providerName => {
				eventRaised = true;
				return "";
			};
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "InformUnknownMfaTypes" )]
		public void InformUnknownMfaTypesCalled() {
			bool eventRaised = false;
			_bmxCore.InformUnknownMfaTypes += mfaOptions => {
				eventRaised = true;
			};
			// Default mock state includes a MfaType.Unknown MfaOption
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "InformUnknownMfaTypes" )]
		public void InformUnknownMfaTypesIncludesAllUnknownMfa() {
			string[] expected = new MockAuthenticateState().MfaOptions.Where( option => option.Type == MfaType.Unknown )
				.Select( option => option.Name ).ToArray();
			string[] actual = { };

			_bmxCore.InformUnknownMfaTypes += mfaOptions => {
				actual = mfaOptions;
			};

			// Default mock state includes a MfaType.Unknown MfaOption
			_bmxCore.Print( "org" );
			Assert.AreEqual( expected, actual );
		}

		[Test]
		[Category( "PromptMfaType" )]
		public void PromptMfaTypeCalled() {
			bool eventRaised = false;
			_bmxCore.PromptMfaType += mfaOptions => {
				eventRaised = true;
				return 0;
			};
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "PromptMfaType" )]
		public void PromptMfaTypeIncludesAllMfa() {
			string[] expected = new MockAuthenticateState().MfaOptions.Select( option => option.Name ).ToArray();
			string[] actual = { };

			_bmxCore.PromptMfaType += mfaOptions => {
				actual = mfaOptions;
				return 0;
			};

			_bmxCore.Print( "org" );
			Assert.AreEqual( expected, actual );
		}

		[Test]
		[Category( "PromptMfaInput" )]
		public void PromptMfaInputCalled() {
			bool eventRaised = false;
			int challengeOptionIndex = Array.FindIndex( new MockAuthenticateState().MfaOptions,
				option => option.Type == MfaType.Challenge );

			// In the future MFA types which do not require an input string shouldn't prompt the user for input
			_bmxCore.PromptMfaType += mfaOptions => challengeOptionIndex;
			_bmxCore.PromptMfaInput += prompt => {
				eventRaised = true;
				return "";
			};
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "PromptAccountSelection" )]
		public void PromptAccountSelectionCalled() {
			bool eventRaised = false;
			_bmxCore.PromptAccountSelection += accounts => {
				eventRaised = true;
				return 0;
			};
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "PromptAccountSelection" )]
		public void PromptAccountSelectionNotCalledWhenValidAccountName() {
			bool eventRaised = false;
			_bmxCore.PromptAccountSelection += accounts => {
				eventRaised = true;
				return 0;
			};
			_bmxCore.Print( "org", account: new MockAccountState().Accounts[0] );
			Assert.IsFalse( eventRaised );
		}

		[Test]
		[Category( "PromptRoleSelection" )]
		public void PromptRoleSelectionCalled() {
			bool eventRaised = false;
			_bmxCore.PromptRoleSelection += roles => {
				eventRaised = true;
				return 0;
			};
			_bmxCore.Print( "org" );
			Assert.IsTrue( eventRaised );
		}

		[Test]
		[Category( "PromptRoleSelection" )]
		public void PromptRoleSelectionNotCalledWhenValidRoleName() {
			bool eventRaised = false;
			_bmxCore.PromptRoleSelection += roles => {
				eventRaised = true;
				return 0;
			};
			_bmxCore.Print( "org", role: new MockRoleState().Roles[0] );
			Assert.IsFalse( eventRaised );
		}
	}
}
