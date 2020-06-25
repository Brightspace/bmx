namespace Bmx.Idp.Okta.Models {
	public struct AuthenticateOptions {
		public string Username { get; set; }
		public string Password { get; set; }

		public AuthenticateOptions( string username, string password ) {
			Username = username;
			Password = password;
		}
	}
}
