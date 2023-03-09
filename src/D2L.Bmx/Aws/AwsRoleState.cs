using System.Collections.Generic;
using System.Linq;
namespace D2L.Bmx;

public interface IRoleState {
	string[] Roles { get; }
}

public class AwsRoleState : IRoleState {
	public AwsRoleState( List<AwsRole> awsRoles, string samlString ) {
		AwsRoles = awsRoles;
		SamlString = samlString;
		Roles = AwsRoles.Select( role => role.RoleName ).ToArray();
	}

	public string[] Roles { get; }
	internal List<AwsRole> AwsRoles { get; }
	internal string SamlString { get; }
}
