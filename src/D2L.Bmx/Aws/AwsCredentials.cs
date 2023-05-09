namespace D2L.Bmx.Aws;

internal record AwsCredentials(
	string SessionToken,
	string AccessKeyId,
	string SecretAccessKey,
	DateTime Expiration
);
