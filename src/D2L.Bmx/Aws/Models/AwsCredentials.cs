namespace D2L.Bmx.Aws.Models;

internal record AwsCredentials(
	string SessionToken,
	string AccessKeyId,
	string SecretAccessKey,
	DateTime Expiration,
	int Version = 1
);
