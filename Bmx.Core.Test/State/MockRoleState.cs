using Bmx.Core.State;

namespace Bmx.Core.Test.State {
	public class MockRoleState : IRoleState {
		public MockRoleState( string[] roles = null) {
			Roles = roles ?? new[] {"Dev-Acc1-Owner", "Dev-Acc1-User", "Dev-Acc1-Readonly",};
		}

		public string[] Roles { get; }
	}
}
