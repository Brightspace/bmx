using Bmx.Core.State;

namespace Bmx.Core.Test.State {
	public class MockAccountState : IAccountState {
		public MockAccountState( string[] accounts = null ) {
			Accounts = accounts ?? new[] {"Dev-Acc1", "Prod-Acc1", "Stg-Acc1"};
		}

		public string[] Accounts { get; }
	}
}
