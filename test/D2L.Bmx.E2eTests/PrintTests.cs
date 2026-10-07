namespace D2L.Bmx.E2eTests;

[TestFixture]
[Order( 3 )]
public class PrintTests {
	private string _workDir = null!;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		_workDir = TestHelpers.CreateTempWorkDir();
		Environment.SetEnvironmentVariable( "AWS_SESSION_TOKEN", null );
		Environment.SetEnvironmentVariable( "AWS_ACCESS_KEY_ID", null );
		Environment.SetEnvironmentVariable( "AWS_SECRET_ACCESS_KEY", null );
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		TestHelpers.CleanupTempWorkDir( _workDir );
	}

	private async Task EnsureGlobalConfig() {
		await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --non-interactive"
		);
	}

	[Test, Order( 1 )]
	public async Task P1_Print_NoGlobalConfig_LocalBmxOnly() {
		TestHelpers.RemoveBmxConfigFileOnly();
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}"
		);

		var result = await BmxRunner.RunAsync(
			"print",
			org: TestConfig.OktaOrg,
			user: TestConfig.OktaUser,
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		if( result.HasAuthFailure ) {
			Assert.Inconclusive(
				"Auth failed — MFA token likely expired. Re-run with a fresh token.\n"
				+ $"stderr: {result.Stderr}" );
		}

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );
		Assert.That(
			result.Stdout,
			Does.Contain( "AWS_ACCESS_KEY_ID" ).Or.Contain( "aws_access_key_id" )
		);
	}

	[Test, Order( 2 )]
	[Category( "DSSO" )]
	[Platform( "Win" )]
	public async Task P2_Print_AllFromConfig_DSSO() {
		TestHelpers.RemoveBmxConfigFileOnly();
		await BmxRunner.RunAsync(
			$"configure --org d2l --user {TestConfig.OktaUser} --non-interactive"
		);
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}"
		);

		var result = await BmxRunner.RunAsync(
			"print",
			workingDirectory: _workDir
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );
		Assert.That(
			result.Stdout,
			Does.Contain( "AWS_ACCESS_KEY_ID" ).Or.Contain( "aws_access_key_id" )
		);
	}

	[Test, Order( 3 )]
	public async Task P3_Print_PromptForRole() {
		await EnsureGlobalConfig();
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}"
		);

		var result = await BmxRunner.RunAsync(
			"print",
			role: TestConfig.AwsRole,
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );
		Assert.That(
			result.Stdout,
			Does.Contain( "AWS_ACCESS_KEY_ID" ).Or.Contain( "aws_access_key_id" )
		);
	}

	[Test, Order( 4 )]
	public async Task P4_Print_PromptForAccountAndRole() {
		await EnsureGlobalConfig();
		TestHelpers.RemoveLocalBmxFile( _workDir );

		var result = await BmxRunner.RunAsync(
			"print",
			account: TestConfig.AwsAccount,
			role: TestConfig.AwsRole,
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );
		Assert.That(
			result.Stdout,
			Does.Contain( "AWS_ACCESS_KEY_ID" ).Or.Contain( "aws_access_key_id" )
		);
	}

	[Test, Order( 5 )]
	public async Task P5_Print_BadAccount_ShouldError() {
		TestHelpers.RemoveLocalBmxFile( _workDir );

		var result = await BmxRunner.RunAsync(
			"print",
			account: "9999",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad account" );
	}

	[Test, Order( 6 )]
	public async Task P6_Print_BadRole_ShouldError() {
		TestHelpers.RemoveLocalBmxFile( _workDir );

		var result = await BmxRunner.RunAsync(
			"print",
			account: TestConfig.AwsAccount,
			role: "9999",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad role" );
	}

	[Test, Order( 7 )]
	public async Task P7_Print_BadAccountInConfig_ShouldError() {
		TestHelpers.WriteLocalBmxFile( _workDir, "account=bad-account" );

		var result = await BmxRunner.RunAsync(
			"print",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad account" );
	}

	[Test, Order( 8 )]
	public async Task P8_Print_BadRoleInConfig_ShouldError() {
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole=bad-role"
		);

		var result = await BmxRunner.RunAsync(
			"print",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad role" );
	}

	[Test, Order( 9 )]
	public async Task P9_Print_CliOverridesConfig() {
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}"
		);

		var result = await BmxRunner.RunAsync(
			$"print --account {TestConfig.AwsAccountAlt} --role {TestConfig.AwsRoleAlt}",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );
		Assert.That(
			result.Stdout,
			Does.Contain( "AWS_ACCESS_KEY_ID" ).Or.Contain( "aws_access_key_id" )
		);
	}

	[Test, Order( 10 )]
	public async Task P10_Print_CacheCredentials() {
		var result1 = await BmxRunner.RunAsync(
			$"print --account {TestConfig.AwsAccount} --role {TestConfig.AwsRole} --format json",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);
		Assert.That( result1.Succeeded, Is.True, $"Run1 stderr: {result1.Stderr}" );

		var result2 = await BmxRunner.RunAsync(
			$"print --account {TestConfig.AwsAccount} --role {TestConfig.AwsRole} --format json --duration 15 --cache",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);
		Assert.That( result2.Succeeded, Is.True, $"Run2 stderr: {result2.Stderr}" );

		var result3 = await BmxRunner.RunAsync(
			$"print --account {TestConfig.AwsAccount} --role {TestConfig.AwsRole} --format json --duration 15 --cache",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);
		Assert.That( result3.Succeeded, Is.True, $"Run3 stderr: {result3.Stderr}" );
		Assert.That( result3.Stdout, Is.EqualTo( result2.Stdout ), "Cached creds should match" );

		var result4 = await BmxRunner.RunAsync(
			$"print --account {TestConfig.AwsAccount} --role {TestConfig.AwsRole} --format json --cache",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);
		Assert.That( result4.Succeeded, Is.True, $"Run4 stderr: {result4.Stderr}" );

		var result5 = await BmxRunner.RunAsync(
			$"print --account {TestConfig.AwsAccount} --role {TestConfig.AwsRole} --format json --duration 20 --cache",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);
		Assert.That( result5.Succeeded, Is.True, $"Run5 stderr: {result5.Stderr}" );
		Assert.That( result5.Stdout, Is.EqualTo( result4.Stdout ), "Should use cached creds" );
	}
}
