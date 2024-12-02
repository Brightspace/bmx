using Spectre.Console;

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
internal class ConsoleWriter : IConsoleWriter {
	// .NET runtime subscribes to the informal standard from https://no-color.org/. We should too.
	// https://github.com/dotnet/runtime/blob/v9.0.0-preview.6.24327.7/src/libraries/Common/src/System/Console/ConsoleUtils.cs#L32-L34
	private readonly bool _noColor
		= Environment.GetEnvironmentVariable( "NO_COLOR" ) == "1" || !VirtualTerminal.TryEnableOnStderr();

	void IConsoleWriter.WriteParameter( string description, string value, ParameterSource source ) {
		string valueColor = _noColor ? "default" : "cyan2";
		string sourceColor = _noColor ? "default" : "grey";
		AnsiConsole.MarkupLine( $"[default]{description}:[/] [{valueColor}]{value}[/] [{sourceColor}](from {source})[/]" );
	}

	void IConsoleWriter.WriteUpdateMessage( string text ) {
		// Trim entries so we don't have extra `\r` characters on Windows.
		// Splitting on `Environment.NewLine` isn't as safe, because we might also use `\n` on Windows.
		string[] lines = text.Split( '\n', StringSplitOptions.TrimEntries );
		int maxLineLength = lines.Max( l => l.Length );
		string color = _noColor ? "default" : "black on white";
		foreach( string line in lines ) {
			string paddedLine = line.PadRight( maxLineLength );
			AnsiConsole.MarkupLine( $"[{color}]{paddedLine}[/]" );
		}
		Console.Error.WriteLine();
	}

	void IConsoleWriter.WriteWarning( string text ) {
		string color = _noColor ? "default" : "yellow";
		AnsiConsole.MarkupLine( $"[{color}]{text}[/]" );
	}

	void IConsoleWriter.WriteError( string text ) {
		string color = _noColor ? "default" : "red";
		AnsiConsole.MarkupLine( $"[{color}]{text}[/]" );
	}
}
