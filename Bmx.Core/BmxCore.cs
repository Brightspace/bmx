using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bmx.Core.State;

namespace Bmx.Core {
	public class
		BmxCore<TAuthenticateState, TAuthenticatedState, TAccountState, TRoleState> : IBmxCore<TAuthenticateState,
			TAuthenticatedState, TAccountState, TRoleState>
		where TAuthenticateState : IAuthenticateState
		where TAuthenticatedState : IAuthenticatedState
		where TAccountState : IAccountState
		where TRoleState : IRoleState {
		private readonly IIdentityProvider<TAuthenticateState, TAuthenticatedState, TAccountState> _identityProvider;
		private readonly ICloudProvider<TRoleState> _cloudProvider;

		public event PromptUserNameHandler PromptUserName;
		public event PromptUserPasswordHandler PromptUserPassword;
		public event PromptMfaTypeHandler PromptMfaType;
		public event PromptMfaInputHandler PromptMfaInput;
		public event PromptAccountSelectHandler PromptAccountSelection;
		public event PromptRoleSelectionHandler PromptRoleSelection;
		public event InformUnknownMfaTypesHandler InformUnknownMfaTypes;


		public BmxCore( IIdentityProvider<TAuthenticateState, TAuthenticatedState, TAccountState> identityProvider,
			ICloudProvider<TRoleState> cloudProvider ) {
			_identityProvider = identityProvider;
			_cloudProvider = cloudProvider;
		}

		public void Print( string org, string account = null, string role = null, string user = null,
			string output = "powershell" ) {
			DoCoreBmx( org, account, role, user ).Wait();
		}

		private async Task DoCoreBmx( string org, string account = null, string role = null, string user = null ) {
			Debug.Assert( PromptUserName != null, nameof(PromptUserName) + " != null" );
			Debug.Assert( PromptUserPassword != null, nameof(PromptUserPassword) + " != null" );
			Debug.Assert( PromptMfaType != null, nameof(PromptMfaType) + " != null" );
			Debug.Assert( PromptMfaInput != null, nameof(PromptMfaInput) + " != null" );
			Debug.Assert( PromptAccountSelection != null, nameof(PromptAccountSelection) + " != null" );
			Debug.Assert( PromptRoleSelection != null, nameof(PromptRoleSelection) + " != null" );

			// TODO: Remove this stateful behaviour
			_identityProvider.SetOrganization( org );

			if( user == null ) {
				user = PromptUserName( _identityProvider.Name );
			}

			// TODO: Handle case creds wrong / user mia
			TAuthenticateState authState =
				await _identityProvider.Authenticate( user, PromptUserPassword( _identityProvider.Name ) );
			var mfaOptions = authState.MfaOptions;

			// TODO: Identify if non MFA use is possible with current setup for BMX, if so skip MFA steps
			// Okta for example has many MFA types (https://developer.okta.com/docs/reference/api/factors/#factor-type)
			// BMX might not handle all, warn user and try to use as a MFA of challenge type (has token user enters)
			if( mfaOptions.Any( option => option.Type == MfaType.Unknown ) ) {
				InformUnknownMfaTypes?.Invoke( mfaOptions.Where( option => option.Type == MfaType.Unknown )
					.Select( option => option.Name ).ToArray() );
			}

			var selectedMfaIndex = PromptMfaType( mfaOptions.Select( option => option.Name ).ToArray() );
			var selectedMfa = mfaOptions[selectedMfaIndex];

			TAuthenticatedState authenticatedState = default;

			// Handle unknown MFA types like Challenge MFAs
			if( selectedMfa.Type == MfaType.Challenge || selectedMfa.Type == MfaType.Unknown ) {
				// TODO: Handle retry for MFA challenge
				authenticatedState = await _identityProvider.ChallengeMfa( authState, selectedMfaIndex,
					PromptMfaInput( selectedMfa.Name ) );
			} else if( selectedMfa.Type == MfaType.Verify ) {
				// Identical to Code based workflow for now (capture same behaviour as BMX current)

				// TODO: Remove need for input here, Push style flows have no user input on app here
				// ex: https://developer.okta.com/docs/reference/api/authn/#response-example-waiting
				PromptMfaInput( selectedMfa.Name );
				throw new NotImplementedException();
			}


			// TODO: Decouple this more, there is an implicit understanding its an Okta AWS app
			TAccountState accountState = await _identityProvider.GetAccounts( authenticatedState, "amazon_aws" );
			var accounts = accountState.Accounts;
			account = account?.ToLower();

			int selectedAccountIndex = -1;

			if( account != null ) {
				selectedAccountIndex = Array.IndexOf( accounts.Select( s => s.ToLower() ).ToArray(), account );
			}

			if( account == null || selectedAccountIndex == -1 ) {
				selectedAccountIndex = PromptAccountSelection( accounts );
			}

			string accountCredentials =
				await _identityProvider.GetServiceProviderSaml( accountState, selectedAccountIndex );

			TRoleState roleState = _cloudProvider.GetRoles( accountCredentials );
			var roles = roleState.Roles;
			role = role?.ToLower();

			int selectedRoleIndex = -1;

			if( role != null ) {
				selectedRoleIndex = Array.IndexOf( roles.Select( s => s.ToLower() ).ToArray(), role );
			}

			if( role == null || selectedRoleIndex == -1 ) {
				selectedRoleIndex = PromptRoleSelection( roles );
			}

			var tokens = await _cloudProvider.GetTokens( roleState, selectedRoleIndex );
		}
	}
}
