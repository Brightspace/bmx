namespace D2L.Bmx;

internal interface IConsole {
	string? ReadLine();
	void Write( string? value );
	void WriteLine( string? value );
}

internal class ConsoleWrapper : IConsole {
	string? IConsole.ReadLine() {
		return Console.ReadLine();
	}

	void IConsole.Write( string? value ) {
		Console.Write( value );
	}

	void IConsole.WriteLine( string? value ) {
		Console.WriteLine( value );
	}
}
