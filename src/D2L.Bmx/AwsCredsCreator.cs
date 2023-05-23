using D2L.Bmx.Aws;
using D2L.Bmx.Okta;

namespace D2L.Bmx;

internal class AwsCredsCreator {
	private readonly IAwsClient _awsClient;
	private readonly IConsolePrompter _consolePrompter;
	private readonly BmxConfig _config;

	public AwsCredsCreator(
		IAwsClient awsClient,
		IConsolePrompter consolePrompter,
		BmxConfig config
	) {
		_awsClient = awsClient;
		_consolePrompter = consolePrompter;
		_config = config;
	}

	public async Task<AwsCredentials> CreateAwsCredsAsync(
		IOktaApi oktaApi,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive
	) {
		var accountState = await oktaApi.GetAccountsAsync( "amazon_aws" );
		string[] accounts = accountState.Accounts;

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( _config.Account ) ) {
				account = _config.Account;
			} else if( !nonInteractive ) {
				account = _consolePrompter.PromptAccount( accounts );
			} else {
				throw new BmxException( "Account value was not provided" );
			}
		}

		string accountCredentials = await oktaApi.GetAccountAsync( accountState, account );
		var roleState = _awsClient.GetRoles( accountCredentials );
		string[] roles = roleState.Roles;

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( _config.Role ) ) {
				role = _config.Role;
			} else if( !nonInteractive ) {
				role = _consolePrompter.PromptRole( roles );
			} else {
				throw new BmxException( "Role value was not provided" );
			}
		}

		if( duration is null or 0 ) {
			if( _config.DefaultDuration is not ( null or 0 ) ) {
				duration = _config.DefaultDuration;
			} else {
				duration = 60;
			}
		}

		return await _awsClient.GetTokensAsync( roleState, role, duration.Value );
	}
}
