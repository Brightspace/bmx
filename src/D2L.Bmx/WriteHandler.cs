using D2L.Bmx.Aws;
using D2L.Bmx.Okta;

namespace D2L.Bmx;

internal class WriteHandler {
	private readonly IBmxConfigProvider _configProvider;
	private readonly IOktaApi _oktaApi;
	private readonly IAwsClient _awsClient;
	public WriteHandler( IBmxConfigProvider configProvider, IOktaApi oktaApi, IAwsClient awsClient ) {
		_configProvider = configProvider;
		_oktaApi = oktaApi;
		_awsClient = awsClient;
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
		var authState = Authenticator.AuthenticateAsync( org, user, nomask, _oktaApi );

		// TODO: replace placeholder values with actual values
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

		if( duration is null ) {
			if( config.DefaultDuration is not null ) {
				duration = config.DefaultDuration;
			} else {
				duration = 60;
			}
		}

		// check if profile flag has been set
		if( string.IsNullOrEmpty( profile ) ) {
			if( !string.IsNullOrEmpty( config.Profile ) ) {
				profile = config.Profile;
			} else {
				profile = ConsolePrompter.PromptProfile();
			}
		}

		// TODO: Replace with call to function to get AWS credentials and write them to credentials file
		Console.WriteLine( string.Join( '\n',
			$"Org: {org}",
			$"Profile: {profile}",
			$"User: {user}",
			$"Account: {account}",
			$"Role: {role}",
			$"Duration: {duration}",
			$"nomask: {nomask}" ) );

	}
}
