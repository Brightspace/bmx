using System.Diagnostics;
using System.Management.Automation;
using D2L.Bmx.Aws;
using Microsoft.PowerShell;

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
			var current = Process.GetCurrentProcess();
			var parent = ProcessCodeMethods.GetParentProcess( new PSObject( current ) ) as Process;
			string? parentProcName = parent?.ProcessName;
			switch( parentProcName ) {
				case "pwsh":
					PrintPowershell( awsCreds );
					break;
				case "bash":
				case "zsh":
					PrintBash( awsCreds );
					break;
				default:
					if( OperatingSystem.IsWindows() ) {
						PrintPowershell( awsCreds );
					} else if( OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ) {
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
