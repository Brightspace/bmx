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
		string? output
	) {
		var oktaApi = await oktaAuth.AuthenticateAsync( org, user, nonInteractive );
		var awsCreds = await awsCredsCreator.CreateAwsCredsAsync( oktaApi, account, role, duration, nonInteractive );

		if( string.Equals( output, "bash", StringComparison.OrdinalIgnoreCase ) ) {
			PrintBash( awsCreds );
		} else if( string.Equals( output, "powershell", StringComparison.OrdinalIgnoreCase ) ) {
			PrintPowershell( awsCreds );
		} else if( string.Equals( output, "json", StringComparison.OrdinalIgnoreCase ) ) {
			PrintJson( awsCreds );
		} else {
			if( OperatingSystem.IsWindows() ) {
				PrintPowershell( awsCreds );
			} else if( OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() ) {
				PrintBash( awsCreds );
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
