namespace D2L.Bmx.E2eTests;

[TestFixture]
[Order( 4 )]
public class WriteTests {
	private string _workDir = null!;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		_workDir = TestHelpers.CreateTempWorkDir();
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		TestHelpers.RemoveBmxTestProfiles();
		TestHelpers.CleanupTempWorkDir( _workDir );
	}

	[SetUp]
	public void SetUp() {
		TestHelpers.RemoveBmxTestProfiles();
	}

	private async Task EnsureGlobalConfig( string org = "" ) {
		if( string.IsNullOrEmpty( org ) ) {
			org = TestConfig.OktaOrg;
		}
		await BmxRunner.RunAsync(
			$"configure --org {org} --user {TestConfig.OktaUser} --non-interactive"
		);
	}

	[Test, Order( 1 )]
	public async Task W1_Write_NoGlobalConfig_AllPrompted() {
		TestHelpers.RemoveBmxConfigFileOnly();
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}\nprofile=bmx-test-w1"
		);

		var result = await BmxRunner.RunAsync(
			"write",
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

		var verifyResult = await RunAwsStsAsync( "bmx-test-w1" );
		Assert.That( verifyResult.ExitCode, Is.EqualTo( 0 ), $"AWS verify failed: {verifyResult.Stderr}" );
	}

	[Test, Order( 2 )]
	[Category( "DSSO" )]
	[Platform( "Win" )]
	public async Task W2_Write_AllFromConfig_DSSO() {
		TestHelpers.RemoveBmxConfigFileOnly();
		await BmxRunner.RunAsync(
			$"configure --org d2l --user {TestConfig.OktaUser} --non-interactive"
		);
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}\nprofile=bmx-test-w2"
		);

		var result = await BmxRunner.RunAsync(
			"write",
			workingDirectory: _workDir
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		var verifyResult = await RunAwsStsAsync( "bmx-test-w2" );
		Assert.That( verifyResult.ExitCode, Is.EqualTo( 0 ), $"AWS verify failed: {verifyResult.Stderr}" );
	}

	[Test, Order( 3 )]
	public async Task W3_Write_PromptProfile() {
		await EnsureGlobalConfig();
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}"
		);

		var result = await BmxRunner.RunAsync(
			"write",
			profile: "bmx-test-w3",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );

		var verifyResult = await RunAwsStsAsync( "bmx-test-w3" );
		Assert.That( verifyResult.ExitCode, Is.EqualTo( 0 ), $"AWS verify failed: {verifyResult.Stderr}" );
	}

	[Test, Order( 4 )]
	public async Task W4_Write_PromptAccountRoleProfile() {
		await EnsureGlobalConfig();
		TestHelpers.RemoveLocalBmxFile( _workDir );

		var result = await BmxRunner.RunAsync(
			"write",
			account: TestConfig.AwsAccount,
			role: TestConfig.AwsRole,
			profile: "bmx-test-w4",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );

		var verifyResult = await RunAwsStsAsync( "bmx-test-w4" );
		Assert.That( verifyResult.ExitCode, Is.EqualTo( 0 ), $"AWS verify failed: {verifyResult.Stderr}" );
	}

	[Test, Order( 5 )]
	public async Task W5_Write_BadAccount_ShouldError() {
		TestHelpers.RemoveLocalBmxFile( _workDir );

		var result = await BmxRunner.RunAsync(
			"write",
			account: "9999",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad account" );
	}

	[Test, Order( 6 )]
	public async Task W6_Write_BadRole_ShouldError() {
		TestHelpers.RemoveLocalBmxFile( _workDir );

		var result = await BmxRunner.RunAsync(
			"write",
			account: TestConfig.AwsAccount,
			role: "9999",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad role" );
	}

	[Test, Order( 7 )]
	public async Task W7_Write_BadAccountInConfig_ShouldError() {
		TestHelpers.WriteLocalBmxFile( _workDir, "account=bad-account" );

		var result = await BmxRunner.RunAsync(
			"write",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad account" );
	}

	[Test, Order( 8 )]
	public async Task W8_Write_BadRoleInConfig_ShouldError() {
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole=bad-role"
		);

		var result = await BmxRunner.RunAsync(
			"write",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.HasError, Is.True, "Should fail with bad role" );
	}

	[Test, Order( 9 )]
	public async Task W9_Write_CliOverridesConfig() {
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}\nprofile=bmx-test-w9"
		);

		var result = await BmxRunner.RunAsync(
			$"write --account {TestConfig.AwsAccountAlt} --role {TestConfig.AwsRoleAlt}",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );

		var verifyResult = await RunAwsStsAsync( "bmx-test-w9" );
		Assert.That( verifyResult.ExitCode, Is.EqualTo( 0 ), $"AWS verify failed: {verifyResult.Stderr}" );
	}

	[Test, Order( 10 )]
	public async Task W10_Write_UseCredentialProcess() {
		await EnsureGlobalConfig();
		TestHelpers.WriteLocalBmxFile(
			_workDir,
			$"account={TestConfig.AwsAccount}\nrole={TestConfig.AwsRole}\nprofile=bmx-test-w10"
		);

		var result = await BmxRunner.RunAsync(
			"write --use-credential-process",
			password: TestConfig.OktaPassword,
			mfaResponse: TestConfig.MfaResponse,
			workingDirectory: _workDir
		);

		Assert.That( result.Succeeded, Is.True, $"stderr: {result.Stderr}" );

		var verifyResult = await RunAwsStsAsync( "bmx-test-w10" );
		Assert.That( verifyResult.ExitCode, Is.EqualTo( 0 ), $"AWS verify failed: {verifyResult.Stderr}" );
	}

	private static async Task<BmxResult> RunAwsStsAsync( string profile ) {
		var psi = new System.Diagnostics.ProcessStartInfo {
			FileName = "aws",
			Arguments = $"sts get-caller-identity --profile {profile}",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using var process = new System.Diagnostics.Process { StartInfo = psi };
		process.Start();

		var stdoutTask = process.StandardOutput.ReadToEndAsync();
		var stderrTask = process.StandardError.ReadToEndAsync();

		using var cts = new CancellationTokenSource( 30_000 );
		try {
			await process.WaitForExitAsync( cts.Token );
		} catch( OperationCanceledException ) {
			try { process.Kill( entireProcessTree: true ); } catch { }
			return new BmxResult( -1, "", "Timed out" );
		}

		return new BmxResult(
			process.ExitCode,
			await stdoutTask,
			await stderrTask
		);
	}
}
