namespace D2L.Bmx;

internal class ConfigureHandler( IBmxConfigProvider configProvider, IConsolePrompter consolePrompter ) {
	public void Handle(
		string? org,
		string? user,
		int? duration,
		bool nonInteractive
	) {

		if( string.IsNullOrEmpty( org ) ) {
			if( nonInteractive ) {
				throw new BmxException( "Org value was not provided" );
			}
			org = consolePrompter.PromptOrg();
		}

		if( string.IsNullOrEmpty( user ) ) {
			if( nonInteractive ) {
				throw new BmxException( "User value was not provided" );
			}
			user = consolePrompter.PromptUser();
		}

		if( duration is null ) {
			duration = nonInteractive ? 60 : consolePrompter.PromptDuration();
		}

		BmxConfig config = new(
			Org: org,
			User: user,
			Account: null,
			Role: null,
			Profile: null,
			Duration: duration
		);
		configProvider.SaveConfiguration( config );
		Console.WriteLine( "Your configuration has been created. Okta sessions will now also be cached." );
	}
}
