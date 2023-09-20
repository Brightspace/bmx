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
		if( cache && !File.Exists( BmxPaths.CONFIG_FILE_NAME ) ) {
			throw new BmxException(
				"BMX global config file not found. Will not cache credentials on shared use machines. If you own this machine, run bmx configure."
			);
		}

		if( string.IsNullOrEmpty( account ) ) {
			if( !string.IsNullOrEmpty( config.Account ) ) {
				account = config.Account;
			}
		}

		if( string.IsNullOrEmpty( role ) ) {
			if( !string.IsNullOrEmpty( config.Role ) ) {
				role = config.Role;
			}
		}

		if( duration is null or 0 ) {
			if( config.Duration is not ( null or 0 ) ) {
				duration = config.Duration;
			} else {
				duration = 60;
			}
		}

		if( cache ) {
			if( string.IsNullOrEmpty( account ) || string.IsNullOrEmpty( role ) ) {
				throw new BmxException( "Account & Role must be provided when using cached AWS credentials" );
			}

			var cachedCredentials = awsCredentialCache.GetCredentials(
				org: oktaApi.Org,
				user: oktaApi.User,
				accountName: account,
				roleName: role,
				duration: duration.Value
			);

			if( cachedCredentials is not null ) {
				return cachedCredentials;
			}
		}

		//cache this one?
		OktaApp[] awsApps = await oktaApi.Api.GetAwsAccountAppsAsync();

		if( string.IsNullOrEmpty( account ) ) {
			if( !nonInteractive ) {
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
			if( !nonInteractive ) {
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

		var credentials = await awsClient.GetTokensAsync(
			samlResponse,
			selectedRoleData,
			duration.Value
		);

		if( cache ) {
			awsCredentialCache.SetCredentials(
				org: oktaApi.Org,
				user: oktaApi.User,
				accountName: selectedAwsApp.Label,
				roleName: selectedRoleData.RoleName,
				credentials: credentials
			);
		}

		return credentials;
	}
}
