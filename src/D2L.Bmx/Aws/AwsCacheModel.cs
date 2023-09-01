namespace D2L.Bmx.Aws;
internal record AwsCacheModel(
	string Org,
	string User,
	string RoleArn,
	//TODO: Cache Expire time now should be a key
	AwsCredentials Credentials
);
