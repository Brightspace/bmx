namespace D2L.Bmx.Aws;

internal class AwsRoleState {
	public string?[] Roles { get; }
	public List<AwsRole> AwsRoles { get; }
	public string SamlString { get; }

	public AwsRoleState( List<AwsRole> awsRoles, string samlString ) {
		AwsRoles = awsRoles;
		SamlString = samlString;
		Roles = AwsRoles.Select( role => role.RoleName ).ToArray();
	}
}
