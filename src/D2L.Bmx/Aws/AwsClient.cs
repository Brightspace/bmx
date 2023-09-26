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

		try {
			var authResp = await stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest {
				PrincipalArn = role.PrincipalArn,
				RoleArn = role.RoleArn,
				SAMLAssertion = samlAssertion,
				DurationSeconds = durationInMinutes * 60,
			} );

			return new AwsCredentials(
				SessionToken: authResp.Credentials.SessionToken,
				AccessKeyId: authResp.Credentials.AccessKeyId,
				SecretAccessKey: authResp.Credentials.SecretAccessKey,
				Expiration: authResp.Credentials.Expiration.ToUniversalTime()
			);
		} catch( Exception ex ) {
			if( ex.Message == "The requested DurationSeconds exceeds the MaxSessionDuration set for this role." ) {
				throw new BmxException( "Duration exceeds the MaxSessionDuration for this role. Lower it in config/parameter", ex );
			}
			throw new BmxException( "Error AWS failed to grab credentials", ex );
		}
	}
}
