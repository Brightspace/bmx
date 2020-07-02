using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bmx.Core {
	public interface ICloudProvider {
		void SetSamlToken( string encodedSaml );
		string[] GetRoles();
		Task<Dictionary<string, string>> GetTokens( int selectedRoleIndex );
	}
}
