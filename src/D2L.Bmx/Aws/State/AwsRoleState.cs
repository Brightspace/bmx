using D2L.Bmx.Aws.Models;

namespace D2L.Bmx.Aws.State;

internal record AwsRoleState( List<AwsRole> AwsRoles, string SamlString ) {
	public List<AwsRole> AwsRoles = AwsRoles;
	public string SamlString = SamlString;
	public string[] Roles => AwsRoles.Select( role => role.RoleName ).ToArray();
}
