using Amazon.Runtime.CredentialManagement;

namespace D2L.Bmx;

internal class WriteHandler(
	OktaAuthenticator oktaAuth,
	AwsCredsCreator awsCredsCreator,
	IConsolePrompter consolePrompter,
	BmxConfig config
) {
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
		var oktaApi = await oktaAuth.AuthenticateAsync( org, user, nonInteractive );
		var awsCreds = await awsCredsCreator.CreateAwsCredsAsync( oktaApi, account, role, duration, nonInteractive );

		if( string.IsNullOrEmpty( profile ) ) {
			if( !string.IsNullOrEmpty( config.Profile ) ) {
				profile = config.Profile;
			} else {
				profile = consolePrompter.PromptProfile();
			}
		}

		if( !string.IsNullOrEmpty( output ) && !Path.IsPathRooted( output ) ) {
			output = "./" + output;
		}
		var credentialsFile = new SharedCredentialsFile( output );

		var profileOptions = new CredentialProfileOptions {
			Token = awsCreds.SessionToken,
			AccessKey = awsCreds.AccessKeyId,
			SecretKey = awsCreds.SecretAccessKey,
		};
		credentialsFile.RegisterProfile( new CredentialProfile( profile, profileOptions ) );
	}
}
