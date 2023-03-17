namespace D2L.Bmx.Aws;

internal record AwsRole(
	string RoleName,
	string PrincipalArn,
	string? RoleArn
);
