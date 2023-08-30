using System.Text;
using Amazon.Runtime.CredentialManagement;
using IniParser;

namespace D2L.Bmx;

internal class WriteHandler(
	OktaAuthenticator oktaAuth,
	AwsCredsCreator awsCredsCreator,
	IConsolePrompter consolePrompter,
	BmxConfig config,
	FileIniDataParser parser
) {
	public async Task HandleAsync(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive,
		string? output,
		string? profile,
		int? useCacheTimeLimit
	) {
		var oktaApi = await oktaAuth.AuthenticateAsync( org, user, nonInteractive, ignoreCache: false );
		var awsCreds = await awsCredsCreator.CreateAwsCredsAsync(
			oktaApi, account, role, duration, nonInteractive, useCacheTimeLimit
		);

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
		string credentialsFilePath = new SharedCredentialsFile( output ).FilePath;
		string credentialsFolderPath = Path.GetDirectoryName( credentialsFilePath )
			?? throw new BmxException( "Invalid AWS credentials file path" );
		if( !Directory.Exists( credentialsFolderPath ) ) {
			Directory.CreateDirectory( credentialsFolderPath );
		}
		if( !File.Exists( credentialsFilePath ) ) {
			using( File.Create( credentialsFilePath ) ) { };
		}

		var data = parser.ReadFile( credentialsFilePath );
		if( !data.Sections.ContainsSection( profile ) ) {
			data.Sections.AddSection( profile );
		}
		data[profile]["aws_access_key_id"] = awsCreds.AccessKeyId;
		data[profile]["aws_secret_access_key"] = awsCreds.SecretAccessKey;
		data[profile]["aws_session_token"] = awsCreds.SessionToken;

		parser.WriteFile( credentialsFilePath, data, new UTF8Encoding( false ) );
	}
}
