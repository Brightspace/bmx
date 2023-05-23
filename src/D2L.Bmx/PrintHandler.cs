using D2L.Bmx.Aws;
using D2L.Bmx.Okta;

namespace D2L.Bmx;

internal class PrintHandler {

	private readonly IBmxConfigProvider _configProvider;
	private readonly IOktaApi _oktaApi;
	private readonly IAwsClient _awsClient;
	private readonly OktaAuthenticator _oktaAuthenticator;
	private readonly IConsolePrompter _consolePrompter;

	public PrintHandler(
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
		bool nonInteractive,
		string? output
	) {

		var config = _configProvider.GetConfiguration();

		// ask user to input org if org flag isn't set
		if( string.IsNullOrEmpty( org ) ) {
			if( !string.IsNullOrEmpty( config.Org ) ) {
				org = config.Org;
			} else if( !nonInteractive ) {
				org = _consolePrompter.PromptOrg();
			} else {
				throw new BmxException( "Org value was not provided" );
			}
		}

		// ask user to input username if user flag isn't set
		if( string.IsNullOrEmpty( user ) ) {
			if( !string.IsNullOrEmpty( config.User ) ) {
				user = config.User;
			} else if( !nonInteractive ) {
				user = _consolePrompter.PromptUser();
			} else {
				throw new BmxException( "User value was not provided" );
			}
		}

		// Asks for user password input, or logs them in through caches
		var authState = await _oktaAuthenticator.AuthenticateAsync( org, user, nonInteractive, _oktaApi );

		var accountState = await _oktaApi.GetAccountsAsync( authState, "amazon_aws" );
		string[] accounts = accountState.Accounts;

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account;
			} else if( !nonInteractive ) {
				account = _consolePrompter.PromptAccount( accounts );
			} else {
				throw new BmxException( "Account value was not provided" );
			}
		}

		string accountCredentials = await _oktaApi.GetAccountAsync( accountState, account );
		var roleState = _awsClient.GetRoles( accountCredentials );
		string[] roles = roleState.Roles;

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( config.Role ) ) {
				role = config.Role;
			} else if( !nonInteractive ) {
				role = _consolePrompter.PromptRole( roles );
			} else {
				throw new BmxException( "Role value was not provided" );
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

		if( string.Equals( output, "bash", StringComparison.OrdinalIgnoreCase ) ) {
			PrintBash( tokens );
		} else if( string.Equals( output, "powershell", StringComparison.OrdinalIgnoreCase ) ) {
			PrintPowershell( tokens );
		} else if( string.Equals( output, "json", StringComparison.OrdinalIgnoreCase ) ) {
			PrintJson( tokens );
		} else {
			if( OperatingSystem.IsWindows() ) {
				PrintPowershell( tokens );
			} else if( OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() ) {
				PrintBash( tokens );
			}
		}
	}

	private static void PrintBash( AwsCredentials credentials ) {
		Console.WriteLine( $"""
			export AWS_SESSION_TOKEN='{credentials.SessionToken}'
			export AWS_ACCESS_KEY_ID='{credentials.AccessKeyId}'
			export AWS_SECRET_ACCESS_KEY='{credentials.SecretAccessKey}'
			""" );
	}

	private static void PrintPowershell( AwsCredentials credentials ) {
		Console.WriteLine( $"""
			$env:AWS_SESSION_TOKEN='{credentials.SessionToken}';
			$env:AWS_ACCESS_KEY_ID='{credentials.AccessKeyId}';
			$env:AWS_SECRET_ACCESS_KEY='{credentials.SecretAccessKey}';
			""" );
	}

	private static void PrintJson( AwsCredentials credentials ) {
		Console.WriteLine( $$"""
		{
			"Version": 1,
			"AccessKeyId": "{{credentials.AccessKeyId}}",
			"SecretAccessKey": "{{credentials.SecretAccessKey}}",
			"SessionToken": "{{credentials.SessionToken}}",
			"Expiration": "{{credentials.Expiration:o}}"
		}
		""" );
	}
}
