
namespace D2L.Bmx;

internal class ConfigureHandler(
	IBmxConfigProvider configProvider,
	IConsolePrompter consolePrompter
) {
	public void Handle(
		string? org,
		string? user,
		int? duration,
		bool nonInteractive,
		bool? passwordless
	) {

		if( string.IsNullOrEmpty( org ) && !nonInteractive ) {
			org = consolePrompter.PromptOrg( allowEmptyInput: true );
		}

		if( string.IsNullOrEmpty( user ) && !nonInteractive ) {
			user = consolePrompter.PromptUser( allowEmptyInput: true );
		}

		if( duration is null && !nonInteractive ) {
			duration = consolePrompter.PromptDuration();
		}

		if( passwordless is null && !nonInteractive ) {
			passwordless = consolePrompter.PromptPasswordless();
		}

		BmxConfig config = new(
			Org: org,
			User: user,
			Account: null,
			Role: null,
			Profile: null,
			Duration: duration,
			Passwordless: passwordless
		);
		configProvider.SaveConfiguration( config );
		Console.WriteLine( "Your configuration has been created. Okta sessions will now also be cached." );
	}
}
