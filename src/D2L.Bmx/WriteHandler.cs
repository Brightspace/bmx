using Amazon.Runtime.CredentialManagement;

namespace D2L.Bmx;

internal class WriteHandler {
	private readonly OktaAuthenticator _oktaAuth;
	private readonly AwsCredsCreator _awsCreds;
	private readonly IConsolePrompter _consolePrompter;
	private readonly BmxConfig _config;

	public WriteHandler(
		OktaAuthenticator oktaAuth,
		AwsCredsCreator awsCreds,
		IConsolePrompter consolePrompter,
		BmxConfig config
	) {
		_oktaAuth = oktaAuth;
		_awsCreds = awsCreds;
		_consolePrompter = consolePrompter;
		_config = config;
	}

	public async Task HandleAsync(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive,
		string? output,
		string? profile
	) {
		var oktaApi = await _oktaAuth.AuthenticateAsync( org, user, nonInteractive );
		var awsCreds = await _awsCreds.CreateAwsCredsAsync( oktaApi, account, role, duration, nonInteractive );

		if( string.IsNullOrEmpty( profile ) ) {
			if( !string.IsNullOrEmpty( _config.Profile ) ) {
				profile = _config.Profile;
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
			Token = awsCreds.SessionToken,
			AccessKey = awsCreds.AccessKeyId,
			SecretKey = awsCreds.SecretAccessKey,
		};
		credentialsFile.RegisterProfile( new CredentialProfile( profile, profileOptions ) );
	}
}
