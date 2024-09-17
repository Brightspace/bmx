namespace D2L.Bmx;

internal class LoginHandler(
	OktaAuthenticator oktaAuth
) {
	public async Task HandleAsync(
		string? org,
		string? user,
		bool experimental
	) {
		if( !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			throw new BmxException(
				"BMX global config file not found. Okta sessions will not be saved. Please run `bmx configure` first."
			);
		}
		await oktaAuth.AuthenticateAsync(
			org,
			user,
			nonInteractive: false,
			ignoreCache: true,
			experimentalBypassBrowserSecurity: experimental
		);
		Console.WriteLine( "Successfully logged in and Okta session has been cached." );
	}
}
