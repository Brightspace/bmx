namespace D2L.Bmx.Aws;
internal record AwsCacheModel(
	string Org,
	string User,
	string RoleArn,
	AwsCredentials Credentials
);
