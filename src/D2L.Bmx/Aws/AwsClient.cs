using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace D2L.Bmx.Aws;

internal interface IAwsClient {
	Task<AwsCredentials> GetTokensAsync( string samlAssertion, AwsRole role, int durationInMinutes );
}

internal class AwsClient( IAmazonSecurityTokenService stsClient ) : IAwsClient {
	async Task<AwsCredentials> IAwsClient.GetTokensAsync(
		string samlAssertion,
		AwsRole role,
		int durationInMinutes
	) {
		//Try get the result
		AwsCredsCache cache = AwsCredsCache();
		AwsCredentials? savedCreds = cache.GetCache( role );
		if( savedCreds != null ) {
			return savedCreds;
		}
		var authResp = await stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest {
			PrincipalArn = role.PrincipalArn,
			RoleArn = role.RoleArn,
			SAMLAssertion = samlAssertion,
			DurationSeconds = durationInMinutes * 60,
		} );
		// What about duration?
		//cache the result
		cache.SaveToFile( role, authResp );
		return new AwsCredentials(
			SessionToken: authResp.Credentials.SessionToken,
			AccessKeyId: authResp.Credentials.AccessKeyId,
			SecretAccessKey: authResp.Credentials.SecretAccessKey,
			Expiration: authResp.Credentials.Expiration.ToUniversalTime()
		);
	}
}
