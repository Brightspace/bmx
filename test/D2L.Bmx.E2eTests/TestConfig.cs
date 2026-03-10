namespace D2L.Bmx.E2eTests;

// Test configuration from environment variables (set by run-e2e-tests.ps1).
internal static class TestConfig {
	public static string OktaOrg =>
		Environment.GetEnvironmentVariable( "BMX_E2E_OKTA_ORG" )
		?? throw new InvalidOperationException( "BMX_E2E_OKTA_ORG not set" );

	public static string OktaUser =>
		Environment.GetEnvironmentVariable( "BMX_E2E_OKTA_USER" )
		?? throw new InvalidOperationException( "BMX_E2E_OKTA_USER not set" );

	public static string OktaPassword =>
		Environment.GetEnvironmentVariable( "BMX_E2E_OKTA_PASSWORD" )
		?? throw new InvalidOperationException( "BMX_E2E_OKTA_PASSWORD not set" );

	public static string? MfaResponse =>
		Environment.GetEnvironmentVariable( "BMX_E2E_MFA_RESPONSE" );

	public static string AwsAccount =>
		Environment.GetEnvironmentVariable( "BMX_E2E_AWS_ACCOUNT" )
		?? "lrn-vulcan";

	public static string AwsRole =>
		Environment.GetEnvironmentVariable( "BMX_E2E_AWS_ROLE" )
		?? "lrn-vulcan-readonly";

	public static string AwsAccountAlt =>
		Environment.GetEnvironmentVariable( "BMX_E2E_AWS_ACCOUNT_ALT" )
		?? "Int-Dev-Ace";

	public static string AwsRoleAlt =>
		Environment.GetEnvironmentVariable( "BMX_E2E_AWS_ROLE_ALT" )
		?? "Int-Dev-Ace-Readonly";
}
