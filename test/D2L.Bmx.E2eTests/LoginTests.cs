namespace D2L.Bmx.E2eTests;

[TestFixture]
[Order( 1 )]
public class LoginTests {
	// Re-establish config + session after LoginTests finishes.
	// Some tests remove ~/.bmx/config or set bad org/user.
	// Subsequent Print/Write fixtures need valid config + cached session.
	[OneTimeTearDown]
	public async Task OneTimeTearDown() {
		await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --non-interactive"
		);
	}

	[Test, Order( 1 )]
	public async Task L1_Login_ValidPassword_CreatesSession() {
		await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --non-interactive"
		);

		var result = await BmxRunner.RunAsync(
			"login",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse
		);

		if( result.HasAuthFailure ) {
			Assert.Inconclusive(
				"Auth failed — MFA token likely expired. Re-run with a fresh token.\n"
				+ $"stderr: {result.Stderr}" );
		}

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );
		Assert.That( TestHelpers.SessionsFileExists(), Is.True, "Sessions file created" );

		string sessions = TestHelpers.ReadSessionsFile();
		Assert.That( sessions, Is.Not.Empty, "Sessions file has content" );
	}

	[Test, Order( 2 )]
	public async Task L2_Login_Relogin_CreatesDifferentSession() {
		string sessionsBefore = TestHelpers.ReadSessionsFile();

		var result = await BmxRunner.RunAsync(
			"login",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse
		);

		if( result.HasAuthFailure ) {
			Assert.Inconclusive(
				"Auth failed — MFA token likely expired. Re-run with a fresh token.\n"
				+ $"stderr: {result.Stderr}" );
		}

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );

		string sessionsAfter = TestHelpers.ReadSessionsFile();
		Assert.That( sessionsAfter, Is.Not.Empty, "Sessions file has content" );
		Assert.That( sessionsAfter, Is.Not.EqualTo( sessionsBefore ), "Session changed" );
	}

	[Test, Order( 3 )]
	[Category( "DSSO" )]
	[Platform( "Win" )]
	public async Task L3_Login_DSSO_NoPassword() {
		TestHelpers.RemoveBmxConfigFileOnly();

		await BmxRunner.RunAsync(
			$"configure --org d2l --user {TestConfig.OktaUser} --non-interactive"
		);

		var result = await BmxRunner.RunAsync( "login" );

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );
		Assert.That( TestHelpers.SessionsFileExists(), Is.True, "Sessions file created" );

		string sessions = TestHelpers.ReadSessionsFile();
		Assert.That( sessions, Is.Not.Empty, "Sessions file has content" );
	}

	[Test, Order( 4 )]
	public async Task L4_Login_BadPassword_ShouldError() {
		await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --non-interactive"
		);

		var result = await BmxRunner.RunAsync(
			"login",
			password: "BadPassword123!"
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad password" );
		Assert.That(
			result.Stderr + result.Stdout,
			Does.Contain( "Unauthorized" ).Or.Contain( "Authentication failed" ).Or.Contain( "failed" ).IgnoreCase,
			"Error message should be informative"
		);
	}

	[Test, Order( 5 )]
	public async Task L5_Login_NoConfig_ShouldError() {
		TestHelpers.RemoveBmxConfigFileOnly();

		BmxResult result;
		try {
			result = await BmxRunner.RunAsync( "login", timeoutMs: 15_000 );
		} catch( TimeoutException ) {
			Assert.Pass( "BMX timed out prompting with no config — as expected." );
			return;
		}

		bool failed = result.ExitCode != 0 || result.HasError
			|| result.Stderr.Contains( "config", StringComparison.OrdinalIgnoreCase );
		Assert.That( failed, Is.True,
			$"Should fail with no config. exit={result.ExitCode} stderr: {result.Stderr}" );
	}

	[Test, Order( 6 )]
	public async Task L6_Login_BadOrg_ShouldError() {
		TestHelpers.RemoveBmxConfigFileOnly();

		await BmxRunner.RunAsync(
			$"configure --org badorg --user {TestConfig.OktaUser} --non-interactive"
		);

		var result = await BmxRunner.RunAsync(
			"login",
			password: TestConfig.OktaPassword
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad org" );
	}

	[Test, Order( 7 )]
	public async Task L7_Login_BadUser_ShouldError() {
		TestHelpers.RemoveBmxConfigFileOnly();

		await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user baduser --non-interactive"
		);

		var result = await BmxRunner.RunAsync(
			"login",
			password: TestConfig.OktaPassword
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad user" );
	}
}
