using System.Collections.Generic;
using System.Threading.Tasks;
using Bmx.Core.State;

namespace Bmx.Core {
	public interface ICloudProvider<TRoleState> where TRoleState : IRoleState {
		TRoleState GetRoles( string encodedSaml );
		Task<Dictionary<string, string>> GetTokens( TRoleState state, int selectedRoleIndex );
	}
}
