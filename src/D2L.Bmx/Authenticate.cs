namespace D2L.Bmx;

internal class Authenticator {
	public static void Authenticate( string? org, string? user, bool nomask ) {
		// TODO: Add authenticating from cache

		Console.Write( "Okta Password: " );
		var password = "";
		if( nomask ) {
			password = Console.ReadLine();
		} else {
			while( true ) {
				var input = Console.ReadKey( intercept: true );
				if( input.Key == ConsoleKey.Enter ) {
					Console.Write( '\n' );
					break;
				} else if( input.Key == ConsoleKey.Backspace && password.Length > 0 ) {
					password = password.Remove( password.Length - 1, 1 );
				} else if( !char.IsControl( input.KeyChar ) ) {
					password += input.KeyChar;
				}
			}
		}

		// TODO: Add authenticaion into Okta Client. If error, print and terminate.
	}
}
