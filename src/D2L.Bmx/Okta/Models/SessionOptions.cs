namespace D2L.Bmx;
public struct SessionOptions {
	public string SessionToken { get; set; }

	public SessionOptions( string sessionToken ) {
		SessionToken = sessionToken;
	}
}
