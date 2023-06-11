namespace D2L.Bmx;

/// <remarks>
/// This exception is used to display an error message to the user.
/// Do not include internal implementation details or very technical info in the exception message.
/// If there's no user facing info to convey, use a different exception type.
/// </remarks>
internal class BmxException : Exception {
	public BmxException( string message ) : base( message ) { }
}
