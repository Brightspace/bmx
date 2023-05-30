namespace D2L.Bmx;

internal class ConfigureHandler {

	private readonly IBmxConfigProvider _configProvider;
	private readonly IConsolePrompter _consolePrompter;

	public ConfigureHandler( IBmxConfigProvider configProvider, IConsolePrompter consolePrompter ) {
		_configProvider = configProvider;
		_consolePrompter = consolePrompter;
	}

	public void Handle(
		string? org,
		string? user,
		int? defaultDuration,
		bool? allowProjectConfigs
	) {

		if( string.IsNullOrEmpty( org ) ) {
			org = _consolePrompter.PromptOrg();
		}

		if( string.IsNullOrEmpty( user ) ) {
			user = _consolePrompter.PromptUser();
		}

		if( defaultDuration is null ) {
			defaultDuration = _consolePrompter.PromptDefaultDuration();
		}

		if( allowProjectConfigs is null ) {
			allowProjectConfigs = _consolePrompter.PromptAllowProjectConfig();
		}

		BmxConfig config = new(
			Org: org,
			User: user,
			Account: null,
			Role: null,
			Profile: null,
			DefaultDuration: defaultDuration,
			AllowProjectConfigs: allowProjectConfigs.GetValueOrDefault() );
		_configProvider.SaveConfiguration( config );
		Console.WriteLine( "Your configuration has been created. Okta sessions will now also be cached." );
	}
}
