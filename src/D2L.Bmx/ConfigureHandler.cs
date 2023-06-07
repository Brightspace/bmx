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
		int? duration
	) {

		if( string.IsNullOrEmpty( org ) ) {
			org = _consolePrompter.PromptOrg();
		}

		if( string.IsNullOrEmpty( user ) ) {
			user = _consolePrompter.PromptUser();
		}

		if( duration is null ) {
			duration = _consolePrompter.PromptDuration();
		}

		BmxConfig config = new(
			Org: org,
			User: user,
			Account: null,
			Role: null,
			Profile: null,
			Duration: duration
		);
		_configProvider.SaveConfiguration( config );
		Console.WriteLine( "Your configuration has been created. Okta sessions will now also be cached." );
	}
}
