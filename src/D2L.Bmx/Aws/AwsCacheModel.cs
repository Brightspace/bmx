namespace D2L.Bmx.Aws;

internal record AwsCacheModel(
	string Org,
	string User,
	string AccountName,
	string RoleName,
	AwsCredentials Credentials
);
