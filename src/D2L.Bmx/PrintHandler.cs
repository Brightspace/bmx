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
		string? format
	) {
		var oktaApi = await _oktaAuth.AuthenticateAsync( org, user, nonInteractive );
		var awsCreds = await _awsCreds.CreateAwsCredsAsync( oktaApi, account, role, duration, nonInteractive );

		if( string.Equals( format, PrintFormat.Bash, StringComparison.OrdinalIgnoreCase ) ) {
			PrintBash( awsCreds );
		} else if( string.Equals( format, PrintFormat.PowerShell, StringComparison.OrdinalIgnoreCase ) ) {
			PrintPowershell( awsCreds );
		} else if( string.Equals( format, PrintFormat.Json, StringComparison.OrdinalIgnoreCase ) ) {
			PrintJson( awsCreds );
		} else {
			throw new UnreachableException();
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
