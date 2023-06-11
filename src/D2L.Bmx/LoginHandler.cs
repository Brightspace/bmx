namespace D2L.Bmx;

internal class LoginHandler {
	private readonly OktaAuthenticator _oktaAuth;

	public LoginHandler( OktaAuthenticator oktaAuth ) {
		_oktaAuth = oktaAuth;
	}

	public async Task HandleAsync(
		string? org,
		string? user
	) {
		if( !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			throw new BmxException(
				"BMX global config file not found. Okta sessions will not be saved. Please run `bmx configure` first."
			);
		}
		_ = await _oktaAuth.AuthenticateAsync( org, user, nonInteractive: false );
	}
}
