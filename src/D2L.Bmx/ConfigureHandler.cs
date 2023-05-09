namespace D2L.Bmx;

internal class ConfigureHandler {

	private readonly IBmxConfigProvider _configProvider;

	public ConfigureHandler( IBmxConfigProvider configProvider ) {
		_configProvider = configProvider;
	}

	public void Handle(
		string? org,
		string? user,
		int? defaultDuration
	) {

		if( string.IsNullOrEmpty( org ) ) {
			org = ConsolePrompter.PromptOrg();
		};

		if( string.IsNullOrEmpty( user ) ) {
			user = ConsolePrompter.PromptUser();
		};

		if( defaultDuration is null ) {
			defaultDuration = ConsolePrompter.PromptDefaultDuration();
		}

		BmxConfig config = new(
			Org: org,
			User: user,
			Account: null,
			Role: null,
			Profile: null,
			DefaultDuration: defaultDuration );
		_configProvider.SaveConfiguration( config );
		Console.WriteLine( "Your configuration has been created. Okta sessions will now also be cached." );
	}
}