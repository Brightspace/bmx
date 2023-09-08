using D2L.Bmx.Aws;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal class AwsCredsCreator(
	IAwsClient awsClient,
	IConsolePrompter consolePrompter,
	IAwsCredentialCache awsCredentialCache,
	BmxConfig config
) {
	public async Task<AwsCredentials> CreateAwsCredsAsync(
		AuthenticatedOktaApi oktaApi,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive,
		bool cache
	) {
		//cache this one?
		OktaApp[] awsApps = await oktaApi.Api.GetAwsAccountAppsAsync();

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account;
			} else if( !nonInteractive ) {
				string[] accounts = awsApps.Select( app => app.Label ).ToArray();
				account = consolePrompter.PromptAccount( accounts );
			} else {
				throw new BmxException( "Account value was not provided" );
			}
		}

		OktaApp selectedAwsApp = Array.Find(
			awsApps,
			app => app.Label.Equals( account, StringComparison.OrdinalIgnoreCase )
		) ?? throw new BmxException( $"Account {account} could not be found" );

		string loginHtml = await oktaApi.Api.GetPageAsync( selectedAwsApp.LinkUrl );
		string samlResponse = HtmlXmlHelper.GetSamlResponseFromLoginPage( loginHtml );
		AwsRole[] rolesData = HtmlXmlHelper.GetRolesFromSamlResponse( samlResponse );

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( config.Role ) ) {
				role = config.Role;
			} else if( !nonInteractive ) {
				string[] roles = rolesData.Select( r => r.RoleName ).ToArray();
				role = consolePrompter.PromptRole( roles );
			} else {
				throw new BmxException( "Role value was not provided" );
			}
		}

		AwsRole selectedRoleData = Array.Find(
			rolesData,
			r => r.RoleName.Equals( role, StringComparison.OrdinalIgnoreCase )
		) ?? throw new BmxException( $"Role {role} could not be found" );

		if( duration is null or 0 ) {
			if( config.Duration is not ( null or 0 ) ) {
				duration = config.Duration;
			} else {
				duration = 60;
			}
		}

		if( cache &&
			awsCredentialCache.GetCredentials(
					org: oktaApi.Org,
					user: oktaApi.User,
					role: selectedRoleData,
					duration: duration.Value
			) is { } cachedCredentials
		) {
			return cachedCredentials;
		}

		var credentials = await awsClient.GetTokensAsync(
			samlResponse,
			selectedRoleData,
			duration.Value
		);

		if( cache ) {
			awsCredentialCache.SetCredentials(
				org: oktaApi.Org,
				user: oktaApi.User,
				role: selectedRoleData,
				credentials: credentials
			);
		}

		return credentials;
	}
}
