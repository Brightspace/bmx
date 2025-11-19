using Spectre.Console;

namespace D2L.Bmx;

internal interface IMessageWriter {
	void WriteParameter( string description, string value, ParameterSource source );
	void WriteUpdateMessage( string text );
	void WriteWarning( string text );
	void WriteError( string text );
}

// We use ANSI escape codes to control colours, because .NET's `Console.ForegroundColor` only targets stdout,
// if stdout is redirected (e.g. typical use case for `bmx print`), we won't get any coloured text on stderr.
// See https://github.com/dotnet/runtime/issues/83146.
// Furthermore, ANSI escape codes give us greater control over the spread of custom background colour.
internal class MessageWriter : IMessageWriter {
	// .NET runtime subscribes to the informal standard from https://no-color.org/. We should too.
	// https://github.com/dotnet/runtime/blob/v9.0.0-preview.6.24327.7/src/libraries/Common/src/System/Console/ConsoleUtils.cs#L32-L34
	private readonly bool _noColor
		= Environment.GetEnvironmentVariable( "NO_COLOR" ) == "1" || !VirtualTerminal.TryEnableOnStderr();
	private readonly IAnsiConsole _ansiConsole = AnsiConsole.Create( new() {
		Out = new AnsiConsoleOutput( Console.Error ),
	} );

	private readonly string _logFilePath = Path.Combine(
		Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
		".bmx",
		"debug.log"
	);

	void IMessageWriter.WriteParameter( string description, string value, ParameterSource source ) {
		string valueColor = _noColor ? "default" : "cyan2";
		string sourceColor = _noColor ? "default" : "grey";
		string message = $"[default]{Markup.Escape( description )}:[/] "
			+ $"[{valueColor}]{Markup.Escape( value )}[/] "
			+ $"[{sourceColor}](from {Markup.Escape( source.ToString() )})[/]";
		_ansiConsole.MarkupLine( message );
	}

	void IMessageWriter.WriteUpdateMessage( string text ) {
		// Trim entries so we don't have extra `\r` characters on Windows.
		// Splitting on `Environment.NewLine` isn't as safe, because we might also use `\n` on Windows.
		string[] lines = text.Split( '\n', StringSplitOptions.TrimEntries );
		int maxLineLength = lines.Max( l => l.Length );
		string color = _noColor ? "default" : "black on white";
		foreach( string line in lines ) {
			string paddedLine = line.PadRight( maxLineLength );
			_ansiConsole.MarkupLine( $"[{color}]{Markup.Escape( paddedLine )}[/]" );
		}
		Console.Error.WriteLine();
	}

	void IMessageWriter.WriteWarning( string text ) {
		string color = _noColor ? "default" : "yellow";
		_ansiConsole.MarkupLine( $"[{color}]{Markup.Escape( text )}[/]" );
		WriteToFile( $"[WARNING] {text}" );
	}

	void IMessageWriter.WriteError( string text ) {
		string color = _noColor ? "default" : "red";
		_ansiConsole.MarkupLine( $"[{color}]{Markup.Escape( text )}[/]" );
		WriteToFile( $"[ERROR] {text}" );
	}

	private void WriteToFile( string text ) {
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
			writer.WriteLine( $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {text}" );
			writer.Flush();
		} catch( Exception ex ) {
			string color = _noColor ? "default" : "red";
			_ansiConsole.MarkupLine( $"[{color}] Error writing to log file: {Markup.Escape( ex.Message )}[/]" );
		}
	}
}
