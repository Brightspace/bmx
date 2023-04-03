using D2L.Bmx.Aws;
using D2L.Bmx.Okta;
namespace D2L.Bmx;

internal class PrintHandler {

	private readonly IBmxConfigProvider _configProvider;
	private readonly IOktaApi _oktaApi;
	private readonly IAwsClient _awsClient;

	public PrintHandler( IBmxConfigProvider configProvider, IOktaApi oktaApi, IAwsClient awsClient ) {
		_configProvider = configProvider;
		_oktaApi = oktaApi;
		_awsClient = awsClient;
	}

	public async Task<bool> HandleAsync(
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
		var authState = await Authenticator.AuthenticateAsync( org, user, nomask, _oktaApi );

		// TODO: replace placeholder values with actual values. Get accounts and roles list from AWS and pass it into the prompter
		// var accounts = new[] { "Dev-Slims", "Dev-Toolmon", "Int-Dev-NDE" };
		var accountState = await _oktaApi.GetAccountsOktaAsync( authState, "amazon_aws" );
		var accounts = accountState.Accounts;

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account.ToLower();
			} else {
				account = ConsolePrompter.PromptAccount( accounts );
			}
		}

		var accountCredentials = await _oktaApi.GetAccountOktaAsync( accountState, account );

		var roleState = _awsClient.GetRoles( accountCredentials );
		var roles = roleState.Roles;

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( config.Role ) ) {
				role = config.Role.ToLower();
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

		var tokens = await _awsClient.GetTokensAsync( roleState, role );

		Console.WriteLine( tokens );

		// TODO: Replace with call to function to get AWS credentials and print them on screen
		Console.WriteLine( string.Join( '\n',
			$"Org: {org}",
			$"User: {user}",
			$"Account: {account}",
			$"Role: {role}",
			$"Duration: {duration}",
			$"nomask: {nomask}" ) );

		return true;
	}
}
