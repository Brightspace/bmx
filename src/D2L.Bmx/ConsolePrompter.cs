using System.Diagnostics;
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
}

internal class ConsolePrompter : IConsolePrompter {
	private const char CTRL_C = '\u0003';
	private const char CTRL_U = '\u0015';
	private const char DEL = '\u007f';
	private static readonly bool IS_WINDOWS = OperatingSystem.IsWindows();

	// When taking user console input on Unix-y platforms, .NET copies the input data to stdout, leading to incorrect
	// `bmx print` output. See https://github.com/dotnet/runtime/issues/22314.
	// We read from stdin (fd = 0) directly on these platforms to bypass .NET's incorrect handling.
	private readonly TextReader _stdinReader =
		IS_WINDOWS
		? Console.In
		: new StreamReader( new FileStream(
			new Microsoft.Win32.SafeHandles.SafeFileHandle( 0, ownsHandle: false ),
			FileAccess.Read
		) );

	string IConsolePrompter.PromptOrg( bool allowEmptyInput ) {
		Console.Error.Write( $"{ParameterDescriptions.Org}{( allowEmptyInput ? " (optional): " : ": " )}" );
		string? org = _stdinReader.ReadLine();
		if( org is null || ( string.IsNullOrWhiteSpace( org ) && !allowEmptyInput ) ) {
			throw new BmxException( "Invalid org input" );
		}

		return org;
	}

	string IConsolePrompter.PromptProfile() {
		Console.Error.Write( $"{ParameterDescriptions.Profile}: " );
		string? profile = _stdinReader.ReadLine();
		if( string.IsNullOrEmpty( profile ) ) {
			throw new BmxException( "Invalid profile input" );
		}

		return profile;
	}

	string IConsolePrompter.PromptUser( bool allowEmptyInput ) {
		Console.Error.Write( $"{ParameterDescriptions.User}{( allowEmptyInput ? " (optional): " : ": " )}" );
		string? user = _stdinReader.ReadLine();
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
		string? input = _stdinReader.ReadLine();
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
		if( !int.TryParse( _stdinReader.ReadLine(), out int index ) || index > accounts.Length || index < 1 ) {
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
		if( !int.TryParse( _stdinReader.ReadLine(), out int index ) || index > roles.Length || index < 1 ) {
			throw new BmxException( "Invalid role selection" );
		}

		return roles[index - 1];
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
		if( !int.TryParse( _stdinReader.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
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
			mfaInput = _stdinReader.ReadLine();
		}

		if( mfaInput is not null ) {
			return mfaInput;
		}
		throw new BmxException( "Invalid MFA Input" );
	}

	private string GetMaskedInput( string prompt ) {
		Func<char> readKey;
		if( IS_WINDOWS ) {
			// On Windows, Console.ReadKey calls native console API, and will fail without a console attached
			if( Console.IsInputRedirected ) {
				Console.Error.WriteLine( """
					====== WARNING ======
					Input to BMX is redirected. Input may be displayed on screen!
					If you're using mintty (with Git Bash, Cygwin, MSYS2 etc.), consider switching
					to Windows Terminal for a better experience.
					If you must use mintty, prefix your bmx command with 'winpty '.
					=====================
					""" );
				readKey = () => (char)_stdinReader.Read();
			} else {
				readKey = () => Console.ReadKey( intercept: true ).KeyChar;
			}
		} else {
			readKey = () => (char)_stdinReader.Read();
		}

		Console.Error.Write( prompt );

		string? originalTerminalSettings = null;
		try {
			if( !IS_WINDOWS ) {
				originalTerminalSettings = GetCurrentTerminalSettings();
				EnableTerminalRawMode();
			}

			var passwordBuilder = new StringBuilder();
			while( true ) {
				char key = readKey();

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
		} finally {
			if( !IS_WINDOWS && !string.IsNullOrEmpty( originalTerminalSettings ) ) {
				SetTerminalSettings( originalTerminalSettings );
			}
		}
	}

	private static string GetCurrentTerminalSettings() {
		var startInfo = new ProcessStartInfo( "stty" );
		startInfo.ArgumentList.Add( "-g" );
		startInfo.RedirectStandardOutput = true;
		using var p = Process.Start( startInfo ) ?? throw new BmxException( "Terminal error" );
		p.WaitForExit();
		return p.StandardOutput.ReadToEnd().Trim();
	}

	private static void EnableTerminalRawMode() {
		using var p = Process.Start( "stty", new[] { "raw", "-echo" } );
		p.WaitForExit();
	}

	private static void SetTerminalSettings( string settings ) {
		using var p = Process.Start( "stty", settings );
		p.WaitForExit();
	}
}
