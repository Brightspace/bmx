namespace Bmx.Idp.Okta.models {
	public struct SessionOptions {
		public string SessionToken { get; set; }

		public SessionOptions( string sessionToken ) {
			SessionToken = sessionToken;
		}
	}
}
