namespace D2L.Bmx;

internal static class ParameterDescriptions {
	public const string Org = "Okta org or domain name";
	public const string User = "Okta username";
	public const string Password = "Okta password";
	public const string Account = "AWS account name";
	public const string Role = "AWS role name";
	public const string Duration = "AWS session duration in minutes";
	public const string Profile = "AWS profile name";
	public const string Output =
		"Custom path to the AWS credentials file, or the AWS config file if '--use-credential-process' is supplied";
	public const string Format = "Output format of AWS credentials";
	public const string NonInteractive = "Run non-interactively without showing any prompts";
	public const string CacheAwsCredentials =
		"Enables Cache for AWS tokens. Implied if '--use-credential-process' is supplied";
	public const string UseCredentialProcess = """
		Write BMX command to AWS profile, so that AWS tools & SDKs using the profile will source credentials from BMX.
		See https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sourcing-external.html.
		""";
}
