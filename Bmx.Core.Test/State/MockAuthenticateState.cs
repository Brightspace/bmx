using Bmx.Core.State;

namespace Bmx.Core.Test.State {
	public class MockAuthenticateState : IAuthenticateState {
		public MockAuthenticateState( MfaOption[] mfaOptions = null ) {
			MfaOptions = mfaOptions ?? new[] {
				new MfaOption( "MFAChallenge", MfaType.Challenge ),
				new MfaOption( "MfaVerify", MfaType.Verify ),
				new MfaOption( "MfaUnknown", MfaType.Unknown ),
			};
		}

		public MfaOption[] MfaOptions { get; }
	}
}
