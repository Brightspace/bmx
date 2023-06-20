using Amazon.Runtime.CredentialManagement;
using IniParser;
using IniParser.Model.Configuration;

namespace D2L.Bmx;

internal class WriteHandler {
	private readonly OktaAuthenticator _oktaAuth;
	private readonly AwsCredsCreator _awsCreds;
	private readonly IConsolePrompter _consolePrompter;
	private readonly BmxConfig _config;
	private readonly FileIniDataParser _parser;

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

		var x = new IniParserConfiguration();


		_parser = new FileIniDataParser();
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

		if( !string.IsNullOrEmpty( output ) && !Path.IsPathRooted( output ) ) {
			output = "./" + output;
		}
		var credentialsFile = new SharedCredentialsFile( output );
		var data = _parser.ReadFile( credentialsFile.FilePath );

		if( !data.Sections.ContainsSection( profile ) ) {
			data.Sections.AddSection( profile );
		}
		data[profile]["aws_access_key_id"] = awsCreds.AccessKeyId;
		data[profile]["aws_secret_access_key"] = awsCreds.SecretAccessKey;
		data[profile]["aws_session_token"] = awsCreds.SessionToken;
		data[profile]["abxc"] = awsCreds.SessionToken;

		_parser.WriteFile( credentialsFile.FilePath, data );
	}
}
