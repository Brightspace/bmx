using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
namespace D2L.Bmx.Aws;

internal interface IAwsClient {
	Task<AwsCredentials> GetTokensAsync(
		string samlAssertion,
		AwsRole role,
		int durationInMinutes,
		int? useCacheTimeLimit,
		string? Org,
		string? User
	);
}

internal class AwsClient( IAmazonSecurityTokenService stsClient ) : IAwsClient {

	async Task<AwsCredentials> IAwsClient.GetTokensAsync(
		string samlAssertion,
		AwsRole role,
		int durationInMinutes,
		int? useCacheTimeLimit,
		string? Org,
		string? User

	) {
		var cache = new AwsCredsCache();
		if( useCacheTimeLimit is not null ) {

			AwsCredentials? savedCreds = cache.GetCachedSession( Org, User, role, useCacheTimeLimit ?? 0 );

			if( savedCreds is not null ) {
				return savedCreds;
			}
		}
		var authResp = await stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest {
			PrincipalArn = role.PrincipalArn,
			RoleArn = role.RoleArn,
			SAMLAssertion = samlAssertion,
			DurationSeconds = durationInMinutes * 60,
		} );
		// What about duration?
		//cache the result
		var credentials = new AwsCredentials(
			SessionToken: authResp.Credentials.SessionToken,
			AccessKeyId: authResp.Credentials.AccessKeyId,
			SecretAccessKey: authResp.Credentials.SecretAccessKey,
			Expiration: authResp.Credentials.Expiration.ToUniversalTime()
		);
		cache.SaveToFile( Org, User, role, credentials );
		return credentials;
	}
}
