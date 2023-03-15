namespace D2L.Bmx.Aws;

internal class AwsRoleState {
	public string?[] Roles { get; }
	internal List<AwsRole> AwsRoles { get; }
	internal string SamlString { get; }

	public AwsRoleState( List<AwsRole> awsRoles, string samlString ) {
		AwsRoles = awsRoles;
		SamlString = samlString;
		Roles = AwsRoles.Select( role => role.RoleName ).ToArray();
	}
}
