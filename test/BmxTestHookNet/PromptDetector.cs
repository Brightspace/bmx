namespace BmxTestHookNet;

internal static class PromptDetector {
	private static readonly Lock s_lock = new();
	private static PromptKind s_currentPrompt = PromptKind.Unknown;
	private static string s_recentText = "";
	private const int MaxRecent = 1024;

	public static void OnWrite( string text ) {
		if( string.IsNullOrEmpty( text ) ) return;

		lock( s_lock ) {
			s_recentText += text;

			// Trim if too long — keep only the tail
			if( s_recentText.Length > MaxRecent ) {
				s_recentText = s_recentText[( s_recentText.Length - MaxRecent )..];
			}

			var detected = Classify( s_recentText );
			if( detected != PromptKind.Unknown ) {
				s_currentPrompt = detected;
				DebugLog.Log( $"Prompt detected: {detected}" );
				// Clear buffer so we don't re-match the same prompt
				s_recentText = "";
			}
		}
	}

	public static PromptKind Consume() {
		lock( s_lock ) {
			var kind = s_currentPrompt;
			s_currentPrompt = PromptKind.Unknown;
			return kind;
		}
	}

	public static PromptKind Peek() {
		lock( s_lock ) {
			return s_currentPrompt;
		}
	}

	private static PromptKind Classify( string text ) {
		if( EndsWith( text, "Okta password: " ) )
			return PromptKind.Password;

		if( EndsWith( text, "Select an available MFA option: " ) )
			return PromptKind.MfaSelect;

		if( EndsWith( text, "PassCode: " ) )
			return PromptKind.MfaResponse;

		if( EndsWith( text, "Select an account: " ) )
			return PromptKind.Account;

		if( EndsWith( text, "Select a role: " ) )
			return PromptKind.Role;

		if( EndsWithAny( text, "Okta org or domain name: ", "Okta org or domain name (optional): " ) )
			return PromptKind.Org;

		if( EndsWithAny( text, "Okta username: ", "Okta username (optional): " ) )
			return PromptKind.User;

		if( EndsWith( text, "AWS profile name: " ) || EndsWith( text, "AWS profile name:" ) )
			return PromptKind.Profile;

		if( Contains( text, "AWS session duration" ) && EndsWithColon( text ) )
			return PromptKind.Duration;

		return PromptKind.Unknown;
	}

	private static bool EndsWith( string haystack, string needle )
		=> haystack.TrimEnd().EndsWith( needle.TrimEnd(), StringComparison.OrdinalIgnoreCase );

	private static bool EndsWithAny( string haystack, params string[] needles ) {
		foreach( string needle in needles ) {
			if( EndsWith( haystack, needle ) ) return true;
		}
		return false;
	}

	private static bool EndsWithColon( string text )
		=> text.TrimEnd().EndsWith( ':' ) || text.TrimEnd().EndsWith( ": " );

	private static bool Contains( string haystack, string needle )
		=> haystack.Contains( needle, StringComparison.OrdinalIgnoreCase );
}
