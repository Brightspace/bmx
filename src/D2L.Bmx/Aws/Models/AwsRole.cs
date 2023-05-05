namespace D2L.Bmx.Aws.Models;

internal record AwsRole(
	string RoleName,
	string PrincipalArn,
	string RoleArn
);
