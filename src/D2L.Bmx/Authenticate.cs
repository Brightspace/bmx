using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Okta.State;

namespace D2L.Bmx;

internal class Authenticator {
	async public static Task<OktaAuthenticatedState> AuthenticateAsync(
		string org,
		string user,
		bool nomask,
		IOktaApi oktaApi ) {
		// TODO: Add authenticating from cache

		Console.Write( "Okta Password: " );
		var password = "";

		// TODO: remove nomask
		while( true ) {
			var input = Console.ReadKey( intercept: true );
			if( input.Key == ConsoleKey.Enter ) {
				Console.Write( '\n' );
				break;
			} else if( input.Key == ConsoleKey.Backspace && password.Length > 0 ) {
				password = password.Remove( password.Length - 1, 1 );
			} else if( !char.IsControl( input.KeyChar ) ) {
				password += input.KeyChar;
			}
		}

		oktaApi.SetOrganization( org );

		var authState = await oktaApi.AuthenticateAsync( new AuthenticateOptions( user, password ) )
			.ConfigureAwait( false );

		OktaAuthenticatedState? authenticatedState = default;

		if( authState.OktaStateToken is not null ) {

			var mfaOptions = authState.MfaOptions;
			var selectedMfaIndex = PromptMfa( mfaOptions );
			var selectedMfa = mfaOptions[selectedMfaIndex - 1];
			string mfaInput = "";

			// TODO: Handle retry
			if( selectedMfa.Type == MfaType.Challenge ) {
				mfaInput = PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Sms ) {
				await oktaApi.IssueMfaChallengeAsync( authState, selectedMfaIndex - 1 );
				mfaInput = PromptMfaInput( "Code" );
			} else if( selectedMfa.Type == MfaType.Question ) {
				mfaInput = PromptMfaInput( "Answer" );
			} else if( selectedMfa.Type == MfaType.Unknown ) {
				// Identical to Code based workflow for now (capture same behaviour as BMX current)
				PromptMfaInput( selectedMfa.Name );
				throw new NotImplementedException();
			}
			authenticatedState = await oktaApi.AuthenticateChallengeMfaAsync( authState, selectedMfaIndex - 1, mfaInput );
		} else if( authState.OktaSessionToken is not null ) {
			authenticatedState = new OktaAuthenticatedState( true, authState.OktaSessionToken );
		}

		if( authenticatedState is not null ) {
			return authenticatedState;
		}

		throw new BmxException( "Authentication Failed" );
	}

	private static int PromptMfa( MfaOption[] mfaOptions ) {

		Console.WriteLine( "MFA Required" );
		for( int i = 0; i < mfaOptions.Length; i++ ) {
			Console.WriteLine( $"[{i + 1}] {mfaOptions[i].Provider}: {mfaOptions[i].Name}" );
		}
		Console.Write( "Select an available MFA option: " );
		if( !int.TryParse( Console.ReadLine(), out int index ) || index > mfaOptions.Length || index < 1 ) {
			throw new BmxException( "Invalid account selection" );
		}

		return index;
	}

	private static string PromptMfaInput( string mfaInputPrompt ) {
		Console.Write( $"{mfaInputPrompt}: " );
		string? mfaInput = Console.ReadLine();

		if( mfaInput is not null ) {
			return mfaInput;
		}
		throw new BmxException( "Invalid Mfa Input" );
	}

}
