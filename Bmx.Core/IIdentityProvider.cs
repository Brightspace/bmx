using System.Threading.Tasks;

namespace Bmx.Core {
	public interface IIdentityProvider {
		public string Name { get; }
		Task<MfaOption[]> Authenticate( string username, string password );
		Task<bool> ChallengeMfa( int selectedMfaIndex, string challengeResponse );
		Task<string[]> GetAccounts( string accountType );
	}
}
