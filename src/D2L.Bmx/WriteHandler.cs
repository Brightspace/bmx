namespace D2L.Bmx;

internal class WriteHandler {

	private readonly IConsole _console;

	public WriteHandler( IConsole console ) {
		_console = console;
	}
	public void Handle(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		bool nomask,
		string? output,
		string? profile
	) {
		// TODO: Get values of org, user, account, role and profile from the config file and assign them locally. This way we can check if
		// values are null to see if they have been set through the commandline or config file

		var unsetFlags = new List<string>();

		// ask user to input org if org flag isn't set
		if( string.IsNullOrEmpty( org ) ) {
			unsetFlags.Add( "\"org\"" );
		};

		// check if profile flag has been set
		if( string.IsNullOrEmpty( profile ) ) {
			unsetFlags.Add( "\"profile\"" );
		}

		if( unsetFlags.Count != 0 ) {
			_console.Write( "Error: required flag(s) " );
			for( int i = 0; i < unsetFlags.Count; i++ ) {
				_console.Write( unsetFlags.ElementAt( i ) );
				if( i < unsetFlags.Count - 1 ) {
					_console.Write( ", " );
				} else {
					_console.WriteLine( " not set" );
				}
			}
			// Also print the output of the help command
			return;
		}

		// ask user to input username if user flag isn't set
		if( string.IsNullOrEmpty( user ) ) {
			_console.Write( "Okta Username: " );
			// there is currently no input validation for the username in the golang build, but it can be added
			user = _console.ReadLine();
		};

		// Asks for user password input, or logs them in through caches
		Authenticator.Authenticate( org, user, nomask );

		// TODO: replace placeholder values with actual values
		var accounts = new[] { "Dev-Slims", "Dev-Toolmon", "Int-Dev-NDE" };
		var roles = new[] { "Dev-Slims-ReadOnly", "Dev-Slims-Admin" };

		if( string.IsNullOrEmpty( account ) ) {
			_console.WriteLine( "Available accounts:" );
			for( int i = 0; i < accounts.Length; i++ ) {
				_console.WriteLine( $"[{i + 1}] {accounts[i]}" );
			}
			_console.Write( "Select an account: " );
			if( !int.TryParse( _console.ReadLine(), out int index ) || index > accounts.Length || index < 1 ) {
				_console.WriteLine( "Error: Invalid selection" );
				return;
			}
			account = accounts[index - 1];
		}

		if( string.IsNullOrEmpty( role ) ) {
			_console.WriteLine( "Available roles:" );
			for( int i = 0; i < roles.Length; i++ ) {
				_console.WriteLine( $"[{i + 1}] {roles[i]}" );
			}
			_console.Write( "Select a role: " );
			if( !int.TryParse( _console.ReadLine(), out int index ) || index > roles.Length || index < 1 ) {
				_console.WriteLine( "Error: Invalid selection" );
				return;
			}
			role = roles[index - 1];
		}

		// TODO: Replace with call to function to get AWS credentials and write them to credentials file
		_console.WriteLine( string.Join( '\n',
			$"Org: {org}",
			$"Profile: {profile}",
			$"User: {user}",
			$"Account: {account}",
			$"Role: {role}",
			$"Duration: {duration}",
			$"nomask: {nomask}" ) );
	}
}
