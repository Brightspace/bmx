namespace D2L.Bmx;

internal class PrintHandler {

	private readonly IBmxConfigProvider _configProvider;

	public PrintHandler( IBmxConfigProvider configProvider ) {
		_configProvider = configProvider;
	}
	public void Handle(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		bool nomask,
		string? output
	) {

		var config = _configProvider.GetConfiguration();

		// ask user to input org if org flag isn't set
		if( string.IsNullOrEmpty( org ) ) {
			if( !string.IsNullOrEmpty( config.Org ) ) {
				org = config.Org;
			} else {
				org = ConsolePrompter.PromptOrg();
			}
		};

		// ask user to input username if user flag isn't set
		if( string.IsNullOrEmpty( user ) ) {
			if( !string.IsNullOrEmpty( config.User ) ) {
				user = config.User;
			} else {
				user = ConsolePrompter.PromptUser();
			}
		};

		// Asks for user password input, or logs them in through caches
		Authenticator.Authenticate( org, user, nomask );

		// TODO: replace placeholder values with actual values. Get accounts and roles list from AWS and pass it into the prompter
		var accounts = new[] { "Dev-Slims", "Dev-Toolmon", "Int-Dev-NDE" };
		var roles = new[] { "Dev-Slims-ReadOnly", "Dev-Slims-Admin" };

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account;
			} else {
				account = ConsolePrompter.PromptAccount( accounts );
			}
		}

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( config.Role ) ) {
				role = config.Role;
			} else {
				role = ConsolePrompter.PromptRole( roles );
			}
		}

		// if duration equals zero, then the duration flag is not set. Users cannot set duration to 0 because of flag validation
		if( duration == 0 ) {
			if( config.Duration is not null ) {
				duration = config.Duration;
			} else {
				// maybe set as some global variable
				duration = 60;
			}
		}

		// TODO: Replace with call to function to get AWS credentials and print them on screen
		Console.WriteLine( string.Join( '\n',
			$"Org: {org}",
			$"User: {user}",
			$"Account: {account}",
			$"Role: {role}",
			$"Duration: {duration}",
			$"nomask: {nomask}" ) );
	}
}
