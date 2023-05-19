using Amazon.Runtime.CredentialManagement;
using D2L.Bmx.Aws;
using D2L.Bmx.Okta;

namespace D2L.Bmx;

internal class WriteHandler {
	private readonly IBmxConfigProvider _configProvider;
	private readonly IOktaApi _oktaApi;
	private readonly IAwsClient _awsClient;
	private readonly OktaAuthenticator _oktaAuthenticator;
	private readonly IConsolePrompter _consolePrompter;

	public WriteHandler(
		IBmxConfigProvider configProvider,
		IOktaApi oktaApi,
		IAwsClient awsClient,
		OktaAuthenticator oktaAuthenticator,
		IConsolePrompter consolePrompter
	) {
		_configProvider = configProvider;
		_oktaApi = oktaApi;
		_awsClient = awsClient;
		_oktaAuthenticator = oktaAuthenticator;
		_consolePrompter = consolePrompter;
	}

	public async Task HandleAsync(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		string? output,
		string? profile
	) {
		var config = _configProvider.GetConfiguration();

		// ask user to input org if org flag isn't set
		if( string.IsNullOrEmpty( org ) ) {
			if( !string.IsNullOrEmpty( config.Org ) ) {
				org = config.Org;
			} else {
				org = _consolePrompter.PromptOrg();
			}
		}

		// ask user to input username if user flag isn't set
		if( string.IsNullOrEmpty( user ) ) {
			if( !string.IsNullOrEmpty( config.User ) ) {
				user = config.User;
			} else {
				user = _consolePrompter.PromptUser();
			}
		}

		// Asks for user password input, or logs them in through caches
		var authState = await _oktaAuthenticator.AuthenticateAsync( org, user, nonInteractive: false, _oktaApi );

		var accountState = await _oktaApi.GetAccountsAsync( authState, "amazon_aws" );
		string[] accounts = accountState.Accounts;

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account;
			} else {
				account = _consolePrompter.PromptAccount( accounts );
			}
		}

		string accountCredentials = await _oktaApi.GetAccountAsync( accountState, account );
		var roleState = _awsClient.GetRoles( accountCredentials );
		string[] roles = roleState.Roles;

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( config.Role ) ) {
				role = config.Role;
			} else {
				role = _consolePrompter.PromptRole( roles );
			}
		}

		if( duration is null ) {
			if( config.DefaultDuration is not null ) {
				duration = config.DefaultDuration;
			} else {
				duration = 60;
			}
		}

		var tokens = await _awsClient.GetTokensAsync( roleState, role, duration.GetValueOrDefault() );

		// check if profile flag has been set
		if( string.IsNullOrEmpty( profile ) ) {
			if( !string.IsNullOrEmpty( config.Profile ) ) {
				profile = config.Profile;
			} else {
				profile = _consolePrompter.PromptProfile();
			}
		}

		var credentialsFile = new SharedCredentialsFile();
		if( !string.IsNullOrEmpty( output ) ) {
			if( !Path.IsPathRooted( output ) ) {
				output = "./" + output;
			}
			credentialsFile = new SharedCredentialsFile( output );
		}

		var profileOptions = new CredentialProfileOptions {
			Token = tokens.SessionToken,
			AccessKey = tokens.AccessKeyId,
			SecretKey = tokens.SecretAccessKey
		};
		credentialsFile.RegisterProfile( new CredentialProfile( profile, profileOptions ) );
	}
}
