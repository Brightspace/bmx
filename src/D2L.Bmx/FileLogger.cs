namespace D2L.Bmx;

internal class FileLogger {
	private readonly string _logFilePath = Path.Combine(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".bmx",
		"debug.log"
	);

	public void Log( string message ) {
		try {
			string? directory = Path.GetDirectoryName( _logFilePath );
			if( string.IsNullOrEmpty( directory ) ) {
				Console.WriteLine( "Unable to determine log file directory." );
				return;
			}

			var fileOptions = new FileStreamOptions {
				Mode = FileMode.Append,
				Access = FileAccess.Write,
				Share = FileShare.ReadWrite
			};

			if( !OperatingSystem.IsWindows() ) {
				fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
			}

			using var stream = new FileStream( _logFilePath, fileOptions );
			using var writer = new StreamWriter( stream );
			writer.WriteLine( $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {message}" );
			writer.Flush();
		} catch( Exception ex ) {
			Console.WriteLine( $"Error writing to log file: {ex.Message}" );
		}
	}
}
