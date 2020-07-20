using System.Threading.Tasks;
using Bmx.Core.State;

namespace Bmx.Core {
	public interface IIdentityProvider<TAuthenticateState, TAuthenticatedState, TAccountState>
		where TAuthenticateState : IAuthenticateState
		where TAuthenticatedState : IAuthenticatedState
		where TAccountState : IAccountState {
		public string Name { get; }
		void SetOrganization( string organization );
		Task<TAuthenticateState> Authenticate( string username, string password );

		Task<TAuthenticatedState> ChallengeMfa( TAuthenticateState state, int selectedMfaIndex,
			string challengeResponse );

		Task<TAccountState> GetAccounts( TAuthenticatedState state, string accountType );
		Task<string> GetServiceProviderSaml( TAccountState state, int selectedAccountIndex );
	}
}
