namespace D2L.Bmx;

internal class LoginHandler(
	OktaAuthenticator oktaAuth
) {
	public async Task HandleAsync(
		string? org,
		string? user
	) {
		if( !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			throw new BmxException(
				"No Config file found! Okta sessions are not cached without a config file. Please run bmx configure first." );
		}
		await oktaAuth.AuthenticateAsync( org, user, nonInteractive: false, ignoreCache: true );
		Console.WriteLine( "Successfully logged in and Okta session has been cached." );
	}
}
