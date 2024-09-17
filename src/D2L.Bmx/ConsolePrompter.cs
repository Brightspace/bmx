using System.Text;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal interface IConsolePrompter {
	string PromptOrg( bool allowEmptyInput );
	string PromptProfile();
	string PromptUser( bool allowEmptyInput );
	string PromptPassword();
	int? PromptDuration();
	string PromptAccount( string[] accounts );
	string PromptRole( string[] roles );
	OktaMfaFactor SelectMfa( OktaMfaFactor[] mfaOptions );
	string GetMfaResponse( string mfaInputPrompt, bool maskInput );
	bool PromptPasswordless();
}

internal class ConsolePrompter : IConsolePrompter {
	private const char CTRL_C = '\u0003';
	private const char CTRL_U = '\u0015';
	private const char DEL = '\u007f';

	string IConsolePrompter.PromptOrg( bool allowEmptyInput ) {
		Console.Error.Write( $"{ParameterDescriptions.Org}{( allowEmptyInput ? " (optional): " : ": " )}" );
		string? org = Console.ReadLine();
		if( org is null || ( string.IsNullOrWhiteSpace( org ) && !allowEmptyInput ) ) {
			throw new BmxException( "Invalid org input" );
		}

		return org;
	}

	string IConsolePrompter.PromptProfile() {
		Console.Error.Write( $"{ParameterDescriptions.Profile}: " );
		string? profile = Console.ReadLine();
		if( string.IsNullOrEmpty( profile ) ) {
			throw new BmxException( "Invalid profile input" );
		}

		return profile;
	}

	string IConsolePrompter.PromptUser( bool allowEmptyInput ) {
		Console.Error.Write( $"{ParameterDescriptions.User}{( allowEmptyInput ? " (optional): " : ": " )}" );
		string? user = Console.ReadLine();
		if( user is null || ( string.IsNullOrWhiteSpace( user ) && !allowEmptyInput ) ) {
			throw new BmxException( "Invalid user input" );
		}

		return user;
	}

	string IConsolePrompter.PromptPassword() {
		return GetMaskedInput( $"{ParameterDescriptions.Password}: " );
	}

	int? IConsolePrompter.PromptDuration() {
		Console.Error.Write( $"{ParameterDescriptions.Duration} (optional, default: 60): " );
		string? input = Console.ReadLine();
		if( input is null || !int.TryParse( input, out int duration ) || duration <= 0 ) {
			return null;
		}
		return duration;
	}

	string IConsolePrompter.PromptAccount( string[] accounts ) {
		if( accounts.Length == 0 ) {
			throw new BmxException( "No AWS account available" );
		}

		if( accounts.Length == 1 ) {
			Console.Error.WriteLine( $"AWS account: {accounts[0]}" );
			return accounts[0];
		}

		Console.Error.WriteLine( "Available accounts:" );
		for( int i = 0; i < accounts.Length; i++ ) {
			Console.Error.WriteLine( $"[{i + 1}] {accounts[i]}" );
		}
		Console.Error.Write( "Select an account: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > accounts.Length || index < 1 ) {
			throw new BmxException( "Invalid account selection" );
		}

		return accounts[index - 1];
	}

	string IConsolePrompter.PromptRole( string[] roles ) {
		if( roles.Length == 0 ) {
			throw new BmxException( "No role available" );
		}

		if( roles.Length == 1 ) {
			Console.Error.WriteLine( $"Role: {roles[0]}" );
			return roles[0];
		}

		Console.Error.WriteLine( "Available roles:" );
		for( int i = 0; i < roles.Length; i++ ) {
			Console.Error.WriteLine( $"[{i + 1}] {roles[i]}" );
		}
		Console.Error.Write( "Select a role: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > roles.Length || index < 1 ) {
			throw new BmxException( "Invalid role selection" );
		}

		return roles[index - 1];
	}

	bool IConsolePrompter.PromptPasswordless() {
		Console.Error.Write( $"{ParameterDescriptions.Passwordless} (y/n): " );
		string? input = Console.ReadLine();
		if( input is null || input.Length != 1 || ( input[0] != 'y' && input[0] != 'n' ) ) {
			throw new BmxException( "Invalid passwordless input" );
		}
		return input[0] == 'y';
	}

	OktaMfaFactor IConsolePrompter.SelectMfa( OktaMfaFactor[] mfaOptions ) {
		Console.Error.WriteLine( "MFA Required" );

		if( mfaOptions.Length == 0 ) {
			throw new BmxException( "No MFA method have been set up for the current user." );
		}

		if( mfaOptions.Length == 1 ) {
			Console.Error.WriteLine( $"MFA method: {mfaOptions[0].Provider} : {mfaOptions[0].FactorName}" );
			return mfaOptions[0];
		}

		for( int i = 0; i < mfaOptions.Length; i++ ) {
			Console.Error.WriteLine( $"[{i + 1}] {mfaOptions[i].Provider} : {mfaOptions[i].FactorName}" );
		}
		Console.Error.Write( "Select an available MFA option: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
			throw new BmxException( "Invalid MFA selection" );
		}
		return mfaOptions[index - 1];
	}

	string IConsolePrompter.GetMfaResponse( string mfaInputPrompt, bool maskInput ) {
		string? mfaInput;

		if( maskInput ) {
			mfaInput = GetMaskedInput( $"{mfaInputPrompt}: " );
		} else {
			Console.Error.Write( $"{mfaInputPrompt}: " );
			mfaInput = Console.ReadLine();
		}

		if( mfaInput is not null ) {
			return mfaInput;
		}
		throw new BmxException( "Invalid MFA Input" );
	}

	private string GetMaskedInput( string prompt ) {
		Console.Error.Write( prompt );

		// If input is redirected, Console.ReadKey( intercept: true ) doesn't work, because it calls system console/terminal APIs.
		if( Console.IsInputRedirected ) {
			if( OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable( "TERM_PROGRAM" ) == "mintty" ) {
				Console.Error.WriteLine( "\x1b[93m" + """
					====== WARNING ======
					Secret input won't be masked on screen!
					This is because you are using mintty (possibly via Git Bash, Cygwin, MSYS2 etc.).
					Consider switching to Windows Terminal for a better experience.
					If you must use mintty, prefix your bmx command with 'winpty '.
					=====================
					""" + "\x1b[0m" );
			}
			return Console.ReadLine() ?? string.Empty;
		}

		var passwordBuilder = new StringBuilder();
		while( true ) {
			char key = Console.ReadKey( intercept: true ).KeyChar;

			if( key == CTRL_C ) {
				// Ctrl+C should terminate the program.
				// Using an empty string as the exception message because this message is displayed to the user,
				// but the user doesn't need to see anything when they themselves ended the program.
				throw new BmxException( string.Empty );
			}
			if( key == '\n' || key == '\r' ) {
				// when the terminal is in raw mode, writing \r is needed to start the new line properly
				Console.Error.Write( "\r\n" );
				return passwordBuilder.ToString();
			}

			if( key == CTRL_U ) {
				string moveLeftString = new( '\b', passwordBuilder.Length );
				string emptyString = new( ' ', passwordBuilder.Length );
				Console.Error.Write( moveLeftString + emptyString + moveLeftString );
				passwordBuilder.Clear();
			} else
			// The backspace key is received as the DEL character in raw mode
			if( ( key == '\b' || key == DEL ) && passwordBuilder.Length > 0 ) {
				Console.Error.Write( "\b \b" );
				passwordBuilder.Length--;
			} else if( !char.IsControl( key ) ) {
				Console.Error.Write( '*' );
				passwordBuilder.Append( key );
			}
		}
	}
}
