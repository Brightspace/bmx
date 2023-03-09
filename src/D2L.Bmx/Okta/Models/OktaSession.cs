namespace D2L.Bmx;
public struct OktaSession {
	public string Id { get; set; }
	public string UserId { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset ExpiresAt { get; set; }
}
