namespace D2L.Bmx.Aws;

internal record AwsCredentials(
	string AwsSessionToken,
	string AwsAccessKeyId,
	string AwsSecretAccessKey
);
