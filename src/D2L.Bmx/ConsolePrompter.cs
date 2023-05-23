namespace D2L.Bmx;

internal static class ConsolePrompter {

	public static string PromptOrg() {
		Console.Write( "Okta org: " );
		string? org = Console.ReadLine();
		if( string.IsNullOrEmpty( org ) ) {
			throw new BmxException( "Invalid org input" );
		}

		return org;
	}

	public static string PromptProfile() {
		Console.Write( "AWS profile: " );
		string? profile = Console.ReadLine();
		if( string.IsNullOrEmpty( profile ) ) {
			throw new BmxException( "Invalid profile input" );
		}

		return profile;
	}

	public static string PromptUser() {
		Console.Write( "Okta Username: " );
		string? user = Console.ReadLine();
		if( string.IsNullOrEmpty( user ) ) {
			throw new BmxException( "Invalid user input" );
		}

		return user;
	}

	public static int? PromptDefaultDuration() {
		Console.Write( "Default duration of session in minutes (optional, default: 60): " );
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

	public static string PromptAccount( string[] accounts ) {
		Console.WriteLine( "Available accounts:" );
		for( int i = 0; i < accounts.Length; i++ ) {
			Console.WriteLine( $"[{i + 1}] {accounts[i]}" );
		}
		Console.Write( "Select an account: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > accounts.Length || index < 1 ) {
			throw new BmxException( "Invalid account selection" );
		}

		return accounts[index - 1];
	}

	public static string PromptRole( string[] roles ) {
		Console.WriteLine( "Available roles:" );
		for( int i = 0; i < roles.Length; i++ ) {
			Console.WriteLine( $"[{i + 1}] {roles[i]}" );
		}
		Console.Write( "Select a role: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > roles.Length || index < 1 ) {
			throw new BmxException( "Invalid role selection" );
		}

		return roles[index - 1];
	}
}
