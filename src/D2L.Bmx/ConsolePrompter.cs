using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal interface IConsolePrompter {
	string PromptOrg();
	string PromptProfile();
	string PromptUser();
	string PromptPassword();
	int? PromptDefaultDuration();
	string PromptAccount( string[] accounts );
	string PromptRole( string[] roles );
	int PromptMfa( MfaOption[] mfaOptions );
	string PromptMfaInput( string mfaInputPrompt );
}

internal class ConsolePrompter : IConsolePrompter {

	string IConsolePrompter.PromptOrg() {
		Console.Error.Write( "Okta org: " );
		string? org = Console.ReadLine();
		if( string.IsNullOrEmpty( org ) ) {
			throw new BmxException( "Invalid org input" );
		}

		return org;
	}

	string IConsolePrompter.PromptProfile() {
		Console.Error.Write( "AWS profile: " );
		string? profile = Console.ReadLine();
		if( string.IsNullOrEmpty( profile ) ) {
			throw new BmxException( "Invalid profile input" );
		}

		return profile;
	}

	string IConsolePrompter.PromptUser() {
		Console.Error.Write( "Okta Username: " );
		string? user = Console.ReadLine();
		if( string.IsNullOrEmpty( user ) ) {
			throw new BmxException( "Invalid user input" );
		}

		return user;
	}

	string IConsolePrompter.PromptPassword() {
		Console.Error.Write( "Okta Password: " );
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

	int? IConsolePrompter.PromptDefaultDuration() {
		Console.Error.Write( "Default duration of session in minutes (optional, default: 60): " );
		string? input = Console.ReadLine();
		if( input is null || !int.TryParse( input, out int defaultDuration ) || defaultDuration <= 0 ) {
			return null;
		}
		return defaultDuration;
	}

	public static bool PromptAllowProjectConfig() {
		Console.Error.Write( "Allow project configs? (true/false, default: false): " );
		string? input = Console.ReadLine();
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
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > accounts.Length || index < 1 ) {
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
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > roles.Length || index < 1 ) {
			throw new BmxException( "Invalid role selection" );
		}

		return roles[index - 1];
	}

	int IConsolePrompter.PromptMfa( MfaOption[] mfaOptions ) {
		Console.Error.WriteLine( "MFA Required" );
		if( mfaOptions.Length > 1 ) {
			for( int i = 0; i < mfaOptions.Length; i++ ) {
				Console.Error.WriteLine( $"[{i + 1}] {mfaOptions[i].Provider}: {mfaOptions[i].Name}" );
			}
			Console.Error.Write( "Select an available MFA option: " );
			if( !int.TryParse( Console.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
				throw new BmxException( "Invalid account selection" );
			}
			return index;
		} else if( mfaOptions.Length == 0 ) {//idk, is mfaOptions' length guaranteed to be >= 1?
			throw new BmxException( "No MFA method have been set up for the current user." );
		} else {
			Console.Error.WriteLine( $"MFA method: {mfaOptions[0].Provider}: {mfaOptions[0].Name}" );
			return 1;
		}

	}

	string IConsolePrompter.PromptMfaInput( string mfaInputPrompt ) {
		Console.Error.Write( $"{mfaInputPrompt}: " );
		string? mfaInput = Console.ReadLine();

		if( mfaInput is not null ) {
			return mfaInput;
		}
		throw new BmxException( "Invalid Mfa Input" );
	}
}
