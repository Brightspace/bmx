namespace D2L.Bmx;

internal interface IConsoleWriter {
	void WriteParameter( string description, string value, ParameterSource source );
	void WriteUpdateMessage( string text );
	void WriteWarning( string text );
	void WriteError( string text );
}

// We use ANSI escape codes to control colours, because .NET's `Console.ForegroundColor` only targets stdout,
// if stdout is redirected (e.g. typical use case for `bmx print`), we won't get any coloured text on stderr.
// See https://github.com/dotnet/runtime/issues/83146.
// Furthermore, ANSI escape codes give us greater control over the spread of custom background colour.
// TODO: use a library to manage ANSI codes and NO_COLOR?
internal class ConsoleWriter : IConsoleWriter {
	// .NET runtime subscribes to the informal standard from https://no-color.org/. We should too.
	// https://github.com/dotnet/runtime/blob/v9.0.0-preview.6.24327.7/src/libraries/Common/src/System/Console/ConsoleUtils.cs#L32-L34
	private readonly bool _noColor
		= Environment.GetEnvironmentVariable( "NO_COLOR" ) == "1" || !VirtualTerminal.TryEnableOnStderr();

	void IConsoleWriter.WriteParameter( string description, string value, ParameterSource source ) {
		if( _noColor ) {
			Console.Error.WriteLine( $"{description}: {value} (from {source})" );
			return;
		}
		// description: default
		// value: bright cyan
		// source: grey / bright black
		Console.Error.WriteLine( $"\x1b[0m{description}: \x1b[96m{value} \x1b[90m(from {source})\x1b[0m" );
	}

	void IConsoleWriter.WriteUpdateMessage( string text ) {
		if( _noColor ) {
			Console.Error.WriteLine( text );
			Console.Error.WriteLine();
			return;
		}
		string[] lines = text.Split( '\n' );
		int maxLineLength = lines.Max( l => l.Length );
		foreach( string line in lines ) {
			string paddedLine = line.PadRight( maxLineLength );
			Console.Error.WriteLine( $"\x1b[0m\x1b[30;47m{paddedLine}\x1b[0m" );
		}
		Console.Error.WriteLine();
	}

	void IConsoleWriter.WriteWarning( string text ) {
		if( _noColor ) {
			Console.Error.WriteLine( text );
			return;
		}
		// bright yellow - 93
		Console.Error.WriteLine( $"\x1b[0m\x1b[93m{text}\x1b[0m" );
	}

	void IConsoleWriter.WriteError( string text ) {
		if( _noColor ) {
			Console.Error.WriteLine( text );
			return;
		}
		// bright red - 91
		Console.Error.WriteLine( $"\x1b[0m\x1b[91m{text}\x1b[0m" );
	}
}
