namespace D2L.Bmx.Aws;

internal record AwsRoleState(
	List<AwsRole> AwsRoles,
	string SamlString
);
