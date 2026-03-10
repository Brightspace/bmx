namespace D2L.Bmx.E2eTests;

[TestFixture]
[Order( 2 )]
public class ConfigureTests {
	private string _workDir = null!;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		_workDir = TestHelpers.CreateTempWorkDir();
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		TestHelpers.CleanupTempWorkDir( _workDir );
	}

	[Test, Order( 1 )]
	public async Task C1_Configure_NonInteractive_AllArgs() {
		TestHelpers.RemoveBmxConfigFileOnly();

		var result = await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --non-interactive"
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( TestConfig.OktaOrg ), "Config should contain org" );
		Assert.That( config, Does.Contain( TestConfig.OktaUser ), "Config should contain user" );
	}

	[Test, Order( 2 )]
	public async Task C2_Configure_PromptDuration_Skipped() {
		TestHelpers.RemoveBmxConfigFileOnly();

		// Hook will provide empty duration (skip) since BMX_TEST_DURATION is not set
		var result = await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser}",
			duration: ""
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( TestConfig.OktaOrg ) );
		Assert.That( config, Does.Contain( TestConfig.OktaUser ) );
	}

	[Test, Order( 3 )]
	public async Task C3_Configure_AllPrompted() {
		TestHelpers.RemoveBmxConfigFileOnly();

		var result = await BmxRunner.RunAsync(
			"configure",
			org: TestConfig.OktaOrg,
			user: TestConfig.OktaUser,
			duration: "" // skip
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( TestConfig.OktaOrg ) );
		Assert.That( config, Does.Contain( TestConfig.OktaUser ) );
	}

	[Test, Order( 4 )]
	public async Task C4_Configure_UpdateUser_SkipOrgAndDuration() {
		string differentUser = TestConfig.OktaUser + "diff";

		var result = await BmxRunner.RunAsync(
			"configure",
			org: "", // skip — keep existing
			user: differentUser,
			duration: "" // skip
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( TestConfig.OktaOrg ), "Org unchanged" );
		Assert.That( config, Does.Contain( differentUser ), "User changed" );
	}

	[Test, Order( 5 )]
	public async Task C5_Configure_UpdateUser_ViaArg_SkipOrgDuration() {
		string userWithZ = TestConfig.OktaUser + "z";

		var result = await BmxRunner.RunAsync(
			$"configure --user {userWithZ}",
			org: "", // skip
			duration: "" // skip
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( userWithZ ), "User has trailing z" );
	}

	[Test, Order( 6 )]
	public async Task C6_Configure_UpdateOrg_ViaArg_SkipUserDuration() {
		string orgWithZ = TestConfig.OktaOrg + "z";

		var result = await BmxRunner.RunAsync(
			$"configure --org {orgWithZ}",
			user: "", // skip
			duration: "" // skip
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( orgWithZ ), "Org has trailing z" );
	}

	[Test, Order( 7 )]
	public async Task C7_Configure_NonInteractive_WithDuration() {
		TestHelpers.RemoveBmxConfigFileOnly();

		var result = await BmxRunner.RunAsync(
			$"configure --org {TestConfig.OktaOrg} --user {TestConfig.OktaUser} --duration 120"
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( TestConfig.OktaOrg ) );
		Assert.That( config, Does.Contain( TestConfig.OktaUser ) );
		Assert.That( config, Does.Contain( "120" ), "Config should contain duration" );
	}

	[Test, Order( 8 )]
	public async Task C8_Configure_Update_NonInteractive() {
		// Ensure config exists from C7
		string modifiedUser = TestConfig.OktaUser + "z";

		var result = await BmxRunner.RunAsync(
			$"configure --user {modifiedUser} --non-interactive"
		);

		Assert.That( result.ExitCode, Is.EqualTo( 0 ), $"stderr: {result.Stderr}" );

		string config = TestHelpers.ReadBmxConfig();
		Assert.That( config, Does.Contain( modifiedUser ), "Config should contain modified user" );
		Assert.That( config, Does.Contain( TestConfig.OktaOrg ), "Config should still contain org" );
	}
}
