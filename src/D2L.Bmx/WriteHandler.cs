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
		bool cacheAwsCredentials,
		bool useCredentialProcess
	) {
		cacheAwsCredentials = cacheAwsCredentials || useCredentialProcess;

		var oktaApi = await oktaAuth.AuthenticateAsync(
			org: org,
			user: user,
			nonInteractive: nonInteractive,
			ignoreCache: false
		);
		var awsCredsInfo = await awsCredsCreator.CreateAwsCredsAsync(
			oktaApi: oktaApi,
			account: account,
			role: role,
			duration: duration,
			nonInteractive: nonInteractive,
			cache: cacheAwsCredentials
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
		if( string.IsNullOrEmpty( output ) ) {
			output = useCredentialProcess
				? SharedCredentialsFile.DefaultConfigFilePath
				: SharedCredentialsFile.DefaultFilePath;
		}

		string outputFolder = Path.GetDirectoryName( output )
			?? throw new BmxException( "Invalid output path" );
		Directory.CreateDirectory( outputFolder );
		if( !File.Exists( output ) ) {
			using( File.Create( output ) ) { };
		}

		var data = parser.ReadFile( output );
		if( useCredentialProcess ) {
			string sectionName = $"profile {profile}";
			if( !data.Sections.ContainsSection( sectionName ) ) {
				data.Sections.AddSection( sectionName );
			}
			if( File.Exists( SharedCredentialsFile.DefaultFilePath ) ) {
				var defaultCredentialsFile = parser.ReadFile( SharedCredentialsFile.DefaultFilePath );
				if( defaultCredentialsFile.Sections.ContainsSection( profile ) ) {
					defaultCredentialsFile.Sections.RemoveSection( profile );
					parser.WriteFile( SharedCredentialsFile.DefaultFilePath, defaultCredentialsFile );
				}
			}
			data[sectionName]["credential_process"] =
				"bmx print --format json --cache --non-interactive"
				+ $" --org \"{oktaApi.Org}\""
				+ $" --user \"{oktaApi.User}\""
				+ $" --account \"{awsCredsInfo.Account}\""
				+ $" --role \"{awsCredsInfo.Role}\""
				+ $" --duration {awsCredsInfo.Duration}";
		} else {
			if( !data.Sections.ContainsSection( profile ) ) {
				data.Sections.AddSection( profile );
			}
			data[profile]["aws_access_key_id"] = awsCredsInfo.Credentials.AccessKeyId;
			data[profile]["aws_secret_access_key"] = awsCredsInfo.Credentials.SecretAccessKey;
			data[profile]["aws_session_token"] = awsCredsInfo.Credentials.SessionToken;
		}

		parser.WriteFile( output, data, new UTF8Encoding( encoderShouldEmitUTF8Identifier: false ) );
	}
}
