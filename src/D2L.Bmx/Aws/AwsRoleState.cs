namespace D2L.Bmx.Aws;

// internal record AwsRoleState(
// 	List<AwsRole> AwsRoles,
// 	string SamlString
// );

internal record AwsRoleState( List<AwsRole> AwsRoles, string SamlString ) {
	public List<AwsRole> AwsRoles = AwsRoles;
	public string SamlString = SamlString;
	public string[] Roles => AwsRoles.Select( role => role.RoleName ).ToArray();
}
