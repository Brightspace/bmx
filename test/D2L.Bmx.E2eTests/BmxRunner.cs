using System.Diagnostics;

namespace D2L.Bmx.E2eTests;

internal record BmxResult(
	int ExitCode,
	string Stdout,
	string Stderr
) {
	public bool Succeeded => ExitCode == 0 && !HasAuthFailure;

	// BMX doesn't always set a non-zero exit code on failure,
	// so we also check stderr for well-known error patterns.
	public bool HasAuthFailure {
		get {
			string combined = Stderr + Stdout;
			return combined.Contains( "Unauthorized", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "Authentication failed", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "authentication for user", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "Check if org, user, and password is correct", StringComparison.Ordinal )
				|| combined.Contains( "Error verifying MFA", StringComparison.OrdinalIgnoreCase );
		}
	}

	public bool HasError {
		get {
			if( ExitCode != 0 ) return true;
			string combined = Stderr + Stdout;
			return HasAuthFailure
				|| combined.Contains( "Unhandled exception", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "BmxException", StringComparison.Ordinal )
				|| combined.Contains( "No AWS account available", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "No role available", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "Invalid account selection", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "Invalid role selection", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "is not an Okta org", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "does not exist", StringComparison.OrdinalIgnoreCase )
				|| combined.Contains( "could not be found", StringComparison.OrdinalIgnoreCase );
		}
	}
}

// Launches bmx.exe with the hook DLL injected via Detours withdll.exe.
// Credentials are supplied via env vars that the hook DLL reads to respond to prompts.
internal static class BmxRunner {
	private static string? s_bmxExePath;
	private static string? s_hookDllPath;
	private static string? s_withDllPath;

	public static void Init() {
		s_bmxExePath = Environment.GetEnvironmentVariable( "BMX_TEST_BMX_EXE" )
			?? throw new InvalidOperationException(
				"BMX_TEST_BMX_EXE must be set to the path of the published bmx.exe" );

		s_hookDllPath = Environment.GetEnvironmentVariable( "BMX_TEST_HOOK_DLL" )
			?? throw new InvalidOperationException(
				"BMX_TEST_HOOK_DLL must be set to the path of BmxTestHookNet.dll" );

		s_withDllPath = Environment.GetEnvironmentVariable( "BMX_TEST_WITHDLL" )
			?? throw new InvalidOperationException(
				"BMX_TEST_WITHDLL must be set to the path of Detours withdll.exe" );

		if( !File.Exists( s_bmxExePath ) ) {
			throw new FileNotFoundException( $"bmx.exe not found: {s_bmxExePath}" );
		}
		if( !File.Exists( s_hookDllPath ) ) {
			throw new FileNotFoundException( $"Hook DLL not found: {s_hookDllPath}" );
		}
		if( !File.Exists( s_withDllPath ) ) {
			throw new FileNotFoundException( $"withdll.exe not found: {s_withDllPath}" );
		}
	}

	public static async Task<BmxResult> RunWithEnvAsync(
		string arguments,
		Dictionary<string, string?>? envOverrides = null,
		string? workingDirectory = null,
		int timeoutMs = 60_000
	) {
		ArgumentNullException.ThrowIfNull( s_bmxExePath );
		ArgumentNullException.ThrowIfNull( s_hookDllPath );
		ArgumentNullException.ThrowIfNull( s_withDllPath );

		// Hook DLL captures output to these temp files since we can't redirect stdio
		string stdoutFile = Path.GetTempFileName();
		string stderrFile = Path.GetTempFileName();

		var psi = new ProcessStartInfo {
			FileName = s_withDllPath,
			Arguments = $"/d:\"{s_hookDllPath}\" \"{s_bmxExePath}\" {arguments}",
			UseShellExecute = false,
			CreateNoWindow = false,
		};

		if( workingDirectory is not null ) {
			psi.WorkingDirectory = workingDirectory;
		}

		psi.Environment["BMX_TEST_STDOUT_FILE"] = stdoutFile;
		psi.Environment["BMX_TEST_STDERR_FILE"] = stderrFile;

		if( envOverrides is not null ) {
			foreach( var (key, value) in envOverrides ) {
				psi.Environment[key] = value;
			}
		}

		using var process = new Process { StartInfo = psi };
		process.Start();

		bool exited = await WaitForExitAsync( process, timeoutMs );
		if( !exited ) {
			try { process.Kill( entireProcessTree: true ); } catch { }
			CleanupTempFiles( stdoutFile, stderrFile );
			throw new TimeoutException(
				$"BMX command timed out after {timeoutMs}ms: bmx {arguments}" );
		}

		string stdout = ReadAndCleanup( stdoutFile );
		string stderr = ReadAndCleanup( stderrFile );

		return new BmxResult( process.ExitCode, stdout, stderr );
	}

	public static Task<BmxResult> RunAsync(
		string arguments,
		string? org = null,
		string? user = null,
		string? password = null,
		string? mfaResponse = null,
		string? account = null,
		string? role = null,
		string? profile = null,
		string? duration = null,
		bool debug = true,
		string? workingDirectory = null,
		int timeoutMs = 60_000
	) {
		var env = new Dictionary<string, string?> {
			["BMX_TEST_HOOK_DEBUG"] = debug ? "1" : "0",
		};

		var cliArgs = new System.Text.StringBuilder();

		if( org is not null ) {
			env["BMX_TEST_ORG"] = org;
		}
		if( user is not null ) {
			env["BMX_TEST_USER"] = user;
		}
		if( password is not null ) env["BMX_TEST_PASSWORD"] = password;
		if( mfaResponse is not null ) env["BMX_TEST_MFA_RESPONSE"] = mfaResponse;
		if( account is not null ) {
			env["BMX_TEST_ACCOUNT"] = account;
			// Also pass as CLI arg so BMX doesn't show a numbered-list prompt
			if( account.Length > 0 && !arguments.Contains( "--account", StringComparison.OrdinalIgnoreCase ) ) {
				cliArgs.Append( $" --account \"{account}\"" );
			}
		}
		if( role is not null ) {
			env["BMX_TEST_ROLE"] = role;
			if( role.Length > 0 && !arguments.Contains( "--role", StringComparison.OrdinalIgnoreCase ) ) {
				cliArgs.Append( $" --role \"{role}\"" );
			}
		}
		if( profile is not null ) env["BMX_TEST_PROFILE"] = profile;
		if( duration is not null ) env["BMX_TEST_DURATION"] = duration;

		string fullArguments = arguments + cliArgs.ToString();

		return RunWithEnvAsync( fullArguments, env, workingDirectory, timeoutMs );
	}

	private static async Task<bool> WaitForExitAsync( Process process, int timeoutMs ) {
		using var cts = new CancellationTokenSource( timeoutMs );
		try {
			await process.WaitForExitAsync( cts.Token );
			return true;
		} catch( OperationCanceledException ) {
			return false;
		}
	}

	private static string ReadAndCleanup( string path ) {
		try {
			return File.Exists( path ) ? File.ReadAllText( path ) : string.Empty;
		} finally {
			try { File.Delete( path ); } catch { }
		}
	}

	private static void CleanupTempFiles( params string[] paths ) {
		foreach( string path in paths ) {
			try { File.Delete( path ); } catch { }
		}
	}
}
