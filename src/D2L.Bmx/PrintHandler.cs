using System.Diagnostics;
using D2L.Bmx.Aws;

namespace D2L.Bmx;

internal class PrintHandler {
	private readonly OktaAuthenticator _oktaAuth;
	private readonly AwsCredsCreator _awsCreds;

	public PrintHandler(
		OktaAuthenticator oktaAuth,
		AwsCredsCreator awsCreds
	) {
		_oktaAuth = oktaAuth;
		_awsCreds = awsCreds;
	}

	public async Task HandleAsync(
		string? org,
		string? user,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive,
		string? output
	) {
		var oktaApi = await _oktaAuth.AuthenticateAsync( org, user, nonInteractive );
		var awsCreds = await _awsCreds.CreateAwsCredsAsync( oktaApi, account, role, duration, nonInteractive );

		if( string.Equals( output, "bash", StringComparison.OrdinalIgnoreCase ) ) {
			PrintBash( awsCreds );
		} else if( string.Equals( output, "powershell", StringComparison.OrdinalIgnoreCase ) ) {
			PrintPowershell( awsCreds );
		} else if( string.Equals( output, "json", StringComparison.OrdinalIgnoreCase ) ) {
			PrintJson( awsCreds );
		} else {
			string parentProcName = "pwsh";
			if( OperatingSystem.IsWindows() ) {
				parentProcName = WindowsParentProcess.GetParentProcessName();
			} else if( OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() ) {
				parentProcName = GetUnixParentProcessName();
			}

			Console.WriteLine( parentProcName );
			switch( parentProcName ) {
				case "pwsh":
					PrintPowershell( awsCreds );
					break;
				case "bash":
					PrintBash( awsCreds );
					break;
				default:
					PrintPowershell( awsCreds );
					break;
			}
		}
	}

	private static string GetUnixParentProcessName() {
		int bmxProcId = Process.GetCurrentProcess().Id;
		string ppid = File.ReadAllText( $"/proc/{bmxProcId}/stat" ).Split( " " )[3];
		return File.ReadAllText( $"/proc/{ppid}/comm" ).Trim();
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
