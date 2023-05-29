using System.Diagnostics;
using System.Runtime.InteropServices;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal interface IConsolePrompter {
	string PromptOrg();
	string PromptProfile();
	string PromptUser();
	string PromptPassword();
	int? PromptDefaultDuration();
	bool PromptAllowProjectConfig();
	string PromptAccount( string[] accounts );
	string PromptRole( string[] roles );
	OktaMfaFactor SelectMfa( OktaMfaFactor[] mfaOptions );
	string GetMfaResponse( string mfaInputPrompt );
}

internal class ConsolePrompter : IConsolePrompter {
	// When taking user console input on Unix-y platforms, .NET copies the input data to stdout, leading to incorrect
	// `bmx print` output. See https://github.com/dotnet/runtime/issues/22314.
	// We read from stdin (fd = 0) directly on these platforms to bypass .NET's incorrect handling.
	private readonly TextReader _stdinReader =
		RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
		? Console.In
		: new StreamReader( new FileStream(
			new Microsoft.Win32.SafeHandles.SafeFileHandle( 0, ownsHandle: false ),
			FileAccess.Read
		) );

	string IConsolePrompter.PromptOrg() {
		Console.Error.Write( "The tenant name or full domain name of the Okta organization: " );
		string? org = _stdinReader.ReadLine();
		if( string.IsNullOrEmpty( org ) ) {
			throw new BmxException( "Invalid org input" );
		}

		return org;
	}

	string IConsolePrompter.PromptProfile() {
		Console.Error.Write( "AWS profile: " );
		string? profile = _stdinReader.ReadLine();
		if( string.IsNullOrEmpty( profile ) ) {
			throw new BmxException( "Invalid profile input" );
		}

		return profile;
	}

	string IConsolePrompter.PromptUser() {
		Console.Error.Write( "Okta Username: " );
		string? user = _stdinReader.ReadLine();
		if( string.IsNullOrEmpty( user ) ) {
			throw new BmxException( "Invalid user input" );
		}

		return user;
	}

	string IConsolePrompter.PromptPassword() {
		Console.Error.Write( "Okta Password: " );

		if( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			var passwordChars = new List<char>();
			while( true ) {
				ConsoleKeyInfo input = Console.ReadKey( intercept: true );
				if( input.Key == ConsoleKey.Enter ) {
					Console.Error.Write( '\n' );
					break;
				}

				if( input.Key == ConsoleKey.Backspace && passwordChars.Count > 0 ) {
					passwordChars.RemoveAt( passwordChars.Count - 1 );
				} else if( !char.IsControl( input.KeyChar ) ) {
					passwordChars.Add( input.KeyChar );
				}
			}
			return new string( passwordChars.ToArray() );
		}

		string? password;
		try {
			// disable the terminal from echoing user input
			using var stty = Process.Start( "stty", "-echo" );
			stty.WaitForExit();
			password = _stdinReader.ReadLine();
		} finally {
			using var stty = Process.Start( "stty", "echo" );
			stty.WaitForExit();
		}
		Console.Error.WriteLine();
		return password ?? throw new BmxException( "No password entered" );
	}

	int? IConsolePrompter.PromptDefaultDuration() {
		Console.Error.Write( "Default duration of AWS sessions in minutes (optional, default: 60): " );
		string? input = _stdinReader.ReadLine();
		if( input is null || !int.TryParse( input, out int defaultDuration ) || defaultDuration <= 0 ) {
			return null;
		}
		return defaultDuration;
	}

	bool IConsolePrompter.PromptAllowProjectConfig() {
		Console.Error.Write( "Allow project level .bmx config file? (true/false, default: false): " );
		string? input = _stdinReader.ReadLine();
		if( input is null || !bool.TryParse( input, out bool allowProjectConfigs ) ) {
			return false;
		}
		return allowProjectConfigs;
	}

	string IConsolePrompter.PromptAccount( string[] accounts ) {
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
			Console.Error.WriteLine( $"MFA method: {mfaOptions[0].Provider}: {mfaOptions[0].FactorType}" );
			return mfaOptions[0];
		}

		for( int i = 0; i < mfaOptions.Length; i++ ) {
			Console.Error.WriteLine( $"[{i + 1}] {mfaOptions[i].Provider}: {mfaOptions[i].FactorType}" );
		}
		Console.Error.Write( "Select an available MFA option: " );
		if( !int.TryParse( _stdinReader.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
			throw new BmxException( "Invalid account selection" );
		}
		return mfaOptions[index - 1];
	}

	string IConsolePrompter.GetMfaResponse( string mfaInputPrompt ) {
		Console.Error.Write( $"{mfaInputPrompt}: " );
		string? mfaInput = _stdinReader.ReadLine();

		if( mfaInput is not null ) {
			return mfaInput;
		}
		throw new BmxException( "Invalid Mfa Input" );
	}
}
