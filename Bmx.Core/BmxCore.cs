using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Bmx.Core {
	public class BmxCore {
		private readonly IIdentityProvider _identityProvider;
		public event PromptUserNameHandler PromptUserName;
		public event PromptUserPasswordHandler PromptUserPassword;
		public event PromptMfaTypeHandler PromptMfaType;
		public event PromptMfaInputHandler PromptMfaInput;
		public event PromptRoleSelectionHandler PromptRoleSelection;
		public event PromptRoleSelectionHandler InformUnknownMfaTypesHandler;

		private string _account;
		private string _org;
		private string _role;
		private string _user;
		private string _output;

		public BmxCore( IIdentityProvider identityProvider ) {
			_identityProvider = identityProvider;
		}

		public void Print( string account, string org, string role = null, string user = null,
			string output = "powershell" ) {
			_output = output;
			DoCoreBmx( account, org, role, user ).Wait();
		}

		private async Task DoCoreBmx( string account, string org, string role = null, string user = null ) {
			_account = account;
			_org = org;
			_role = role;
			_user = user;

			Debug.Assert( PromptUserName != null, nameof(PromptUserName) + " != null" );
			Debug.Assert( PromptUserPassword != null, nameof(PromptUserPassword) + " != null" );
			Debug.Assert( PromptMfaType != null, nameof(PromptMfaType) + " != null" );
			Debug.Assert( PromptMfaInput != null, nameof(PromptMfaInput) + " != null" );
			Debug.Assert( PromptRoleSelection != null, nameof(PromptRoleSelection) + " != null" );

			if( user == null ) {
				_user = PromptUserName( _identityProvider.Name );
			}

			// TODO: Handle case creds wrong / user mia
			var mfaOptions =
				await _identityProvider.Authenticate( _user, PromptUserPassword( _identityProvider.Name ) );

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
			}
		}
	}
}
