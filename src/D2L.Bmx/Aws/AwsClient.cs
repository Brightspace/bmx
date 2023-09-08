using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
namespace D2L.Bmx.Aws;

internal interface IAwsClient {
	Task<AwsCredentials> GetTokensAsync(
		string samlAssertion,
		AwsRole role,
		int durationInMinutes
	);
}

internal class AwsClient( IAmazonSecurityTokenService stsClient ) : IAwsClient {

	async Task<AwsCredentials> IAwsClient.GetTokensAsync(
		string samlAssertion,
		AwsRole role,
		int durationInMinutes
	) {
		var authResp = await stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest {
			PrincipalArn = role.PrincipalArn,
			RoleArn = role.RoleArn,
			SAMLAssertion = samlAssertion,
			DurationSeconds = durationInMinutes * 60,
		} );

		var credentials = new AwsCredentials(
			SessionToken: authResp.Credentials.SessionToken,
			AccessKeyId: authResp.Credentials.AccessKeyId,
			SecretAccessKey: authResp.Credentials.SecretAccessKey,
			Expiration: authResp.Credentials.Expiration.ToUniversalTime()
		);

		return credentials;
	}
}
