using Amazon.Runtime.CredentialManagement;
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

	public async Task HandleAsync(
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
		var authState = await Authenticator.AuthenticateAsync( org, user, nomask, _oktaApi );

		var accountState = await _oktaApi.GetAccountsOktaAsync( authState, "amazon_aws" );
		var accounts = accountState.Accounts;

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account;
			} else {
				account = ConsolePrompter.PromptAccount( accounts );
			}
		}

		var accountCredentials = await _oktaApi.GetAccountOktaAsync( accountState, account );
		var roleState = _awsClient.GetRoles( accountCredentials );
		var roles = roleState.Roles;

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

		var tokens = await _awsClient.GetTokensAsync( roleState, role, duration.GetValueOrDefault() );

		// check if profile flag has been set
		if( string.IsNullOrEmpty( profile ) ) {
			if( !string.IsNullOrEmpty( config.Profile ) ) {
				profile = config.Profile;
			} else {
				profile = ConsolePrompter.PromptProfile();
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
			Token = tokens.AwsSessionToken,
			AccessKey = tokens.AwsAccessKeyId,
			SecretKey = tokens.AwsSecretAccessKey
		};
		credentialsFile.RegisterProfile( new CredentialProfile( profile, profileOptions ) );
	}
}
