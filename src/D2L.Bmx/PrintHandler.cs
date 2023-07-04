using D2L.Bmx.Aws;

namespace D2L.Bmx;

internal class PrintHandler(
	OktaAuthenticator oktaAuth,
	AwsCredsCreator awsCredsCreator
) {
	public async Task HandleAsync(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive,
		string? format
	) {
		var oktaApi = await oktaAuth.AuthenticateAsync( org, user, nonInteractive, ignoreCache: false );
		var awsCreds = await awsCredsCreator.CreateAwsCredsAsync( oktaApi, account, role, duration, nonInteractive );

		if( string.Equals( format, PrintFormat.Bash, StringComparison.OrdinalIgnoreCase ) ) {
			PrintBash( awsCreds );
		} else if( string.Equals( format, PrintFormat.PowerShell, StringComparison.OrdinalIgnoreCase ) ) {
			PrintPowershell( awsCreds );
		} else if( string.Equals( format, PrintFormat.Json, StringComparison.OrdinalIgnoreCase ) ) {
			PrintJson( awsCreds );
		} else {
			string? procName = null;
			if( OperatingSystem.IsWindows() ) {
				procName = WindowsParentProcess.GetParentProcessName().ToLower();
			} else if( OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ) {
				procName = UnixParentProcess.GetParentProcessName().ToLower();
			}

			switch( procName ) {
				case "pwsh":
				case "powershell":
					PrintPowershell( awsCreds );
					break;
				case "zsh":
				case "bash":
				case "sh":
					PrintBash( awsCreds );
					break;
				default:
					if( OperatingSystem.IsWindows() ) {
						PrintPowershell( awsCreds );
					} else if( OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() ) {
						PrintBash( awsCreds );
					}
					break;
			}
		}
	}

	private static void PrintBash( AwsCredentials credentials ) {
		Console.WriteLine( $"""
			export AWS_SESSION_TOKEN={credentials.SessionToken}
			export AWS_ACCESS_KEY_ID={credentials.AccessKeyId}
			export AWS_SECRET_ACCESS_KEY={credentials.SecretAccessKey}
			""" );
	}

	private static void PrintPowershell( AwsCredentials credentials ) {
		Console.WriteLine( $"""
			$env:AWS_SESSION_TOKEN='{credentials.SessionToken}';
			$env:AWS_ACCESS_KEY_ID='{credentials.AccessKeyId}';
			$env:AWS_SECRET_ACCESS_KEY='{credentials.SecretAccessKey}';
			""" );
	}

	private static void PrintJson( AwsCredentials credentials ) {
		Console.WriteLine( $$"""
		{
			"Version": 1,
			"AccessKeyId": "{{credentials.AccessKeyId}}",
			"SecretAccessKey": "{{credentials.SecretAccessKey}}",
			"SessionToken": "{{credentials.SessionToken}}",
			"Expiration": "{{credentials.Expiration:o}}"
		}
		""" );
	}
}
