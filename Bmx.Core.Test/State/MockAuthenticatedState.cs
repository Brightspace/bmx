using Bmx.Core.State;

namespace Bmx.Core.Test.State {
	public class MockAuthenticatedState : IAuthenticatedState {
		public MockAuthenticatedState( bool successfulAuthentication = true ) {
			SuccessfulAuthentication = successfulAuthentication;
		}

		public bool SuccessfulAuthentication { get; }
	}
}
