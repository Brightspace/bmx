using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Bmx.Core {
	public class BmxCore : IBmxCore {
		private readonly IIdentityProvider _identityProvider;
		private readonly ICloudProvider _cloudProvider;
		public event PromptUserNameHandler PromptUserName;
		public event PromptUserPasswordHandler PromptUserPassword;
		public event PromptMfaTypeHandler PromptMfaType;
		public event PromptMfaInputHandler PromptMfaInput;
		public event PromptAccountSelectHandler PromptAccountSelection;
		public event PromptRoleSelectionHandler PromptRoleSelection;
		public event PromptRoleSelectionHandler InformUnknownMfaTypesHandler;


		public BmxCore( IIdentityProvider identityProvider, ICloudProvider cloudProvider ) {
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

			_identityProvider.SetOrganization( org );

			if( user == null ) {
				user = PromptUserName( _identityProvider.Name );
			}

			// TODO: Handle case creds wrong / user mia
			var mfaOptions =
				await _identityProvider.Authenticate( user, PromptUserPassword( _identityProvider.Name ) );

			// TODO: Identify if non MFA use is possible with current setup for BMX, if so skip MFA steps
			// Okta for example has many MFA types (https://developer.okta.com/docs/reference/api/factors/#factor-type)
			// BMX might not handle all, warn user and try to use as a MFA of challenge type (has token user enters)
			if( mfaOptions.Any( option => option.Type == MfaType.Unknown ) ) {
				InformUnknownMfaTypesHandler?.Invoke( mfaOptions.Where( option => option.Type == MfaType.Unknown )
					.Select( option => option.Name ).ToArray() );
			}

			var selectedMfaIndex = PromptMfaType( mfaOptions.Select( option => option.Name ).ToArray() );
			var selectedMfa = mfaOptions[selectedMfaIndex];

			// Handle unknown MFA types like Challenge MFAs
			if( selectedMfa.Type == MfaType.Challenge || selectedMfa.Type == MfaType.Unknown ) {
				// TODO: Handle retry for MFA challenge
				bool isAuthSuccess =
					await _identityProvider.ChallengeMfa( selectedMfaIndex, PromptMfaInput( selectedMfa.Name ) );
			} else if( selectedMfa.Type == MfaType.Verify ) {
				// Identical to Code based workflow for now (capture same behaviour as BMX current)

				// TODO: Remove need for input here, Push style flows have no user input on app here
				// ex: https://developer.okta.com/docs/reference/api/authn/#response-example-waiting
				PromptMfaInput( selectedMfa.Name );
				throw new NotImplementedException();
			}


			// TODO: Decouple this more, there is an implicit understanding its an Okta AWS app
			var accounts = await _identityProvider.GetAccounts( "amazon_aws" );
			account = account?.ToLower();

			int selectedAccountIndex = -1;

			if( account != null ) {
				selectedAccountIndex = Array.IndexOf( accounts.Select( s => s.ToLower() ).ToArray(), account );
			}

			if( account == null || selectedAccountIndex == -1 ) {
				selectedAccountIndex = PromptAccountSelection( accounts );
			}

			var accountCredentials = await _identityProvider.GetServiceProviderSaml( selectedAccountIndex );

			_cloudProvider.SetSamlToken( accountCredentials );
			var roles = _cloudProvider.GetRoles();
			role = role?.ToLower();

			int selectedRoleIndex = -1;

			if( role != null ) {
				selectedRoleIndex = Array.IndexOf( roles.Select( s => s.ToLower() ).ToArray(), role );
			}

			if( role == null || selectedRoleIndex == -1 ) {
				selectedRoleIndex = PromptRoleSelection( roles );
			}

			var tokens = await _cloudProvider.GetTokens( selectedRoleIndex );
		}
	}
}
