namespace Bmx.Idp.Okta.Models {
	public struct SessionOptions {
		public string SessionToken { get; set; }

		public SessionOptions( string sessionToken ) {
			SessionToken = sessionToken;
		}
	}
}
