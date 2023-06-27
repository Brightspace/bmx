namespace D2L.Bmx;

internal class LoginHandler(
	OktaAuthenticator oktaAuth
) {
	public async Task HandleAsync(
		string? org,
		string? user
	) {
		await oktaAuth.AuthenticateAsync( org, user, nonInteractive: false );
		Console.WriteLine( "Successfully logged in." );
	}
}
