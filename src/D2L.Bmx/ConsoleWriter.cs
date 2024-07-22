namespace D2L.Bmx;

internal interface IConsoleWriter {
	void WriteParameter( string description, string value, ParameterSource source );
}

internal class ConsoleWriter : IConsoleWriter {
	void IConsoleWriter.WriteParameter( string description, string value, ParameterSource source ) {
		Console.ResetColor();
		Console.Error.Write( $"{description}: " );
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.Error.Write( value );
		Console.ForegroundColor = ConsoleColor.DarkGray;
		Console.Error.WriteLine( $" (from {source})" );
		Console.ResetColor();
	}
}
