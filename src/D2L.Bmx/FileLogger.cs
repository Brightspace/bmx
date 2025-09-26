namespace D2L.Bmx;

internal class FileLogger {
	private readonly string _logFilePath = Path.Combine(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".bmx",
		"bmx.log"
	);

	public void Log( string message ) {
		try {
			string? directory = Path.GetDirectoryName( _logFilePath );
			if( string.IsNullOrEmpty( directory ) ) {
				Console.WriteLine( "Unable to determine log file directory." );
				return;
			}
			File.AppendAllText( _logFilePath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}" );
		} catch( Exception ex ) {
			Console.WriteLine( $"Error writing to log file: {ex.Message}" );
		}
	}
}
