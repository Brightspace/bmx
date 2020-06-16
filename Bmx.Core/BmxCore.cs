using System;
using System.Diagnostics;

namespace Bmx.Core {
	public class BmxCore {
		private IIdentityProvider _identityProvider;
		public event PromptUserNameHandler PromptUserName;
		public event PromptUserPasswordHandler PromptUserPassword;
		public event PromptMfaTypeHandler PromptMfaType;
		public event PromptMfaInputHandler PromptMfaInput;
		public event PromptRoleSelectionHandler PromptRoleSelection;

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
			_account = account;
			_org = org;
			_role = role;
			_user = user;
			_output = output;


			Debug.Assert( PromptUserName != null, nameof(PromptUserName) + " != null" );
			Debug.Assert( PromptUserPassword != null, nameof(PromptUserPassword) + " != null" );
			Debug.Assert( PromptMfaType != null, nameof(PromptMfaType) + " != null" );
			Debug.Assert( PromptRoleSelection != null, nameof(PromptRoleSelection) + " != null" );

			if( user == null ) {
				_user = PromptUserName( _identityProvider.Name );
			}

			// TODO: <IDP: Check if user exists here>
			Console.WriteLine( "Check if user exists..." );

			// TODO: <IDP: Auth here>
			Console.WriteLine( $"Creds: u: {_user} p: {PromptUserPassword( _identityProvider.Name )}" );

			// TODO: <IDP: Check MFA mode's available and provide them here
			Console.WriteLine( "Check available MFA modes for user" );

			// TODO: <IDP: MFA Here>
			var mfaType = PromptMfaType( new string[] {"token:software:totp", "other"} );
			var mfaInput = PromptMfaInput( "code:" );
			Console.WriteLine( $"MFA, type: {mfaType} input: {mfaInput}" );

			// TODO: <AWS: Check roles in account here>
			if( role == null ) {
				_role = PromptRoleSelection( new string[] {"Role-1", "ROle-2"} );
			}

			// TODO: <AWS: Get short lived IAM credentials for role>
			Console.Write( $"Short lived credentials for role {_role}" );
		}
	}
}
