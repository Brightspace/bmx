using Amazon;

namespace D2L.Bmx.Aws;
internal record AwsCacheModel(
	string Org,
	string User,
	string AccountName,
	string RoleArn,
	AwsCredentials Credentials
);
