namespace BmxTestHookNet;

internal enum PromptKind {
	Unknown,
	Org,
	User,
	Password,
	MfaSelect,
	MfaResponse,
	Account,
	Role,
	Profile,
	Duration,
}

internal static class CredentialProvider {
	private static string s_org = "";
	private static string s_user = "";
	private static string s_password = "";
	private static string s_mfaResponse = "";
	private static string s_account = "";
	private static string s_role = "";
	private static string s_profile = "";
	private static string s_duration = "";

	public static void Init() {
		s_org = Environment.GetEnvironmentVariable( "BMX_TEST_ORG" ) ?? "";
		s_user = Environment.GetEnvironmentVariable( "BMX_TEST_USER" ) ?? "";
		s_password = Environment.GetEnvironmentVariable( "BMX_TEST_PASSWORD" ) ?? "";
		s_mfaResponse = Environment.GetEnvironmentVariable( "BMX_TEST_MFA_RESPONSE" ) ?? "";
		s_account = Environment.GetEnvironmentVariable( "BMX_TEST_ACCOUNT" ) ?? "";
		s_role = Environment.GetEnvironmentVariable( "BMX_TEST_ROLE" ) ?? "";
		s_profile = Environment.GetEnvironmentVariable( "BMX_TEST_PROFILE" ) ?? "";
		s_duration = Environment.GetEnvironmentVariable( "BMX_TEST_DURATION" ) ?? "";

		bool debug = Environment.GetEnvironmentVariable( "BMX_TEST_HOOK_DEBUG" ) is "1" or "true";
		DebugLog.Init( debug );

		DebugLog.Log( $"Credentials loaded: org={( s_org.Length > 0 ? "(set)" : "(empty)" )}, "
			+ $"user={( s_user.Length > 0 ? "(set)" : "(empty)" )}, "
			+ $"password={( s_password.Length > 0 ? "(set)" : "(empty)" )}" );
	}

	public static string GetResponse( PromptKind kind ) {
		return kind switch {
			PromptKind.Org => s_org,
			PromptKind.User => s_user,
			PromptKind.Password => s_password,
			PromptKind.MfaSelect => "1", // always select first MFA option
			PromptKind.MfaResponse => s_mfaResponse,
			PromptKind.Account => s_account,
			PromptKind.Role => s_role,
			PromptKind.Profile => s_profile,
			PromptKind.Duration => s_duration,
			_ => "",
		};
	}
}
