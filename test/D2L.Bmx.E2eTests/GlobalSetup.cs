namespace D2L.Bmx.E2eTests;

[SetUpFixture]
public class GlobalSetup {
	[OneTimeSetUp]
	public async Task Setup() {
		BmxRunner.Init();

		// Bootstrap an Okta session so all tests can reuse it
		TestHelpers.RemoveBmxConfig();

		var configResult = await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --non-interactive"
		);

		if( !configResult.Succeeded ) {
			throw new InvalidOperationException(
				$"Failed to run bmx configure. Exit code: {configResult.ExitCode}\n"
				+ $"stderr: {configResult.Stderr}\nstdout: {configResult.Stdout}"
			);
		}

		var loginResult = await BmxRunner.RunAsync(
			"login",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse
		);

		if( loginResult.HasAuthFailure ) {
			throw new InvalidOperationException(
				"Okta authentication failed during test setup.\n"
				+ "This usually means the password is wrong or the MFA token has expired.\n"
				+ "Please re-run using the run-e2e-tests.ps1 script which supports\n"
				+ "interactive re-prompting for credentials.\n\n"
				+ $"stderr: {loginResult.Stderr}\nstdout: {loginResult.Stdout}"
			);
		}

		if( !loginResult.Succeeded ) {
			throw new InvalidOperationException(
				$"Failed to bootstrap Okta session. Exit code: {loginResult.ExitCode}\n"
				+ $"stderr: {loginResult.Stderr}\nstdout: {loginResult.Stdout}"
			);
		}

		TestContext.Progress.WriteLine( $"[GlobalSetup] configure stdout: {configResult.Stdout}" );
		TestContext.Progress.WriteLine( $"[GlobalSetup] configure stderr: {configResult.Stderr}" );
		TestContext.Progress.WriteLine( $"[GlobalSetup] login stdout: {loginResult.Stdout}" );
		TestContext.Progress.WriteLine( $"[GlobalSetup] login stderr: {loginResult.Stderr}" );
		TestContext.Progress.WriteLine(
			$"[GlobalSetup] sessions file exists: {TestHelpers.SessionsFileExists()}" );

		if( TestHelpers.SessionsFileExists() ) {
			TestContext.Progress.WriteLine(
				$"[GlobalSetup] sessions file content: {TestHelpers.ReadSessionsFile()}" );
		} else {
			TestContext.Progress.WriteLine(
				$"[GlobalSetup] ~/.bmx contents: {string.Join( ", ", ListBmxDir() )}" );
		}
	}

	private static IEnumerable<string> ListBmxDir() {
		string bmxDir = Path.Combine(
			Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ),
			".bmx" );

		if( !Directory.Exists( bmxDir ) ) {
			return ["<directory does not exist>"];
		}

		return Directory.EnumerateFileSystemEntries( bmxDir, "*", SearchOption.AllDirectories )
			.Select( p => Path.GetRelativePath( bmxDir, p ) );
	}
}
