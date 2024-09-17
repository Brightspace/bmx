using System.Diagnostics;
using System.Text;
using Amazon.Runtime.CredentialManagement;
using IniParser;
using IniParser.Model;

namespace D2L.Bmx;

internal class WriteHandler(
	OktaAuthenticator oktaAuth,
	AwsCredsCreator awsCredsCreator,
	IConsolePrompter consolePrompter,
	IConsoleWriter consoleWriter,
	BmxConfig config,
	FileIniDataParser parser
) {
	// this is different from `Encoding.UTF8` which has the undesirable `encoderShouldEmitUTF8Identifier: true`
	private static readonly UTF8Encoding Utf8 = new();

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
		bool useCredentialProcess,
		bool experimental,
		bool? passwordless
	) {
		cacheAwsCredentials = cacheAwsCredentials || useCredentialProcess;

		var oktaContext = await oktaAuth.AuthenticateAsync(
			org: org,
			user: user,
			nonInteractive: nonInteractive,
			ignoreCache: false,
			experimental: experimental,
			passwordless: passwordless
		);
		var awsCredsInfo = await awsCredsCreator.CreateAwsCredsAsync(
			okta: oktaContext,
			account: account,
			role: role,
			duration: duration,
			nonInteractive: nonInteractive,
			cache: cacheAwsCredentials
		);

		var profileSource = ParameterSource.CliArg;
		if( string.IsNullOrEmpty( profile ) && !string.IsNullOrEmpty( config.Profile ) ) {
			profile = config.Profile;
			profileSource = ParameterSource.Config;
		}
		if( string.IsNullOrEmpty( profile ) ) {
			if( nonInteractive ) {
				throw new BmxException( "Profile value was not provided" );
			}
			profile = consolePrompter.PromptProfile();
		} else if( !nonInteractive ) {
			consoleWriter.WriteParameter( ParameterDescriptions.Profile, profile, profileSource );
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

		string awsConfigFilePath = useCredentialProcess ? output : SharedCredentialsFile.DefaultConfigFilePath;
		var awsConfig = GetParsedIniFileOrNull( awsConfigFilePath );

		string awsCredentialsFilePath = useCredentialProcess ? SharedCredentialsFile.DefaultFilePath : output;
		var awsCredentials = GetParsedIniFileOrNull( awsCredentialsFilePath );

		if( useCredentialProcess ) {
			Debug.Assert( awsConfig is not null );

			if( awsCredentials is not null && awsCredentials.Sections.ContainsSection( profile ) ) {
				awsCredentials.Sections.RemoveSection( profile );
				parser.WriteFile( awsCredentialsFilePath, awsCredentials, Utf8 );
			}

			string sectionName = profile == "default" ? "default" : $"profile {profile}";

			if( !awsConfig.Sections.ContainsSection( sectionName ) ) {
				awsConfig.Sections.AddSection( sectionName );
			}
			RemoveCredentialProviderSettings( awsConfig[sectionName], out _ );
			awsConfig[sectionName]["credential_process"] =
				"bmx print --format json --cache --non-interactive"
				+ $" --org {oktaContext.Org}"
				+ $" --user {oktaContext.User}"
				+ $" --account {awsCredsInfo.Account}"
				+ $" --role {awsCredsInfo.Role}"
				+ $" --duration {awsCredsInfo.Duration}";

			parser.WriteFile( awsConfigFilePath, awsConfig, Utf8 );
		} else {
			Debug.Assert( awsCredentials is not null );

			if( awsConfig is not null ) {
				string sectionName = profile == "default" ? "default" : $"profile {profile}";

				if( awsConfig.Sections.ContainsSection( sectionName ) ) {
					RemoveCredentialProviderSettings( awsConfig[sectionName], out bool foundCredentialProcess );
					if( awsConfig[sectionName].Count == 0 ) {
						awsConfig.Sections.RemoveSection( sectionName );
					}
					parser.WriteFile( awsConfigFilePath, awsConfig, Utf8 );
					if( foundCredentialProcess ) {
						consoleWriter.WriteWarning(
"""
An existing profile with the same name using the `credential_process` setting was found in the default config file.
The setting has been removed, and static credentials will be used for the profile.
To continue using non-static credentials, rerun the command with the --use-credential-process flag.
"""
						);
					}
				}
			}

			if( !awsCredentials.Sections.ContainsSection( profile ) ) {
				awsCredentials.Sections.AddSection( profile );
			}
			awsCredentials[profile]["aws_access_key_id"] = awsCredsInfo.Credentials.AccessKeyId;
			awsCredentials[profile]["aws_secret_access_key"] = awsCredsInfo.Credentials.SecretAccessKey;
			awsCredentials[profile]["aws_session_token"] = awsCredsInfo.Credentials.SessionToken;

			parser.WriteFile( awsCredentialsFilePath, awsCredentials, Utf8 );
		}
	}

	private IniData? GetParsedIniFileOrNull( string path ) {
		return File.Exists( path ) ? parser.ReadFile( path, Utf8 ) : null;
	}

	private static void RemoveCredentialProviderSettings( KeyDataCollection iniKeyData, out bool foundCredentialProcess ) {
		// https://docs.aws.amazon.com/sdkref/latest/guide/settings-reference.html
		iniKeyData.RemoveKey( "aws_access_key_id" );
		iniKeyData.RemoveKey( "aws_secret_access_key" );
		iniKeyData.RemoveKey( "aws_session_token" );
		iniKeyData.RemoveKey( "credential_source" );
		iniKeyData.RemoveKey( "duration_seconds" );
		iniKeyData.RemoveKey( "external_id" );
		iniKeyData.RemoveKey( "mfa_serial" );
		iniKeyData.RemoveKey( "role_arn" );
		iniKeyData.RemoveKey( "role_session_name" );
		iniKeyData.RemoveKey( "source_profile" );
		iniKeyData.RemoveKey( "sso_account_id" );
		iniKeyData.RemoveKey( "sso_region" );
		iniKeyData.RemoveKey( "sso_registration_scopes" );
		iniKeyData.RemoveKey( "sso_role_name" );
		iniKeyData.RemoveKey( "sso_start_url" );
		iniKeyData.RemoveKey( "web_identity_token_file" );

		foundCredentialProcess = iniKeyData.RemoveKey( "credential_process" );
	}
}
