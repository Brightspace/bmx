using D2L.Bmx.Aws;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal record AwsCredentialsInfo(
	string Account,
	string Role,
	int Duration,
	AwsCredentials Credentials
);

internal class AwsCredsCreator(
	IAwsClient awsClient,
	IConsolePrompter consolePrompter,
	IAwsCredentialCache awsCredentialCache,
	BmxConfig config
) {
	public async Task<AwsCredentialsInfo> CreateAwsCredsAsync(
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

		if( string.IsNullOrEmpty( account ) && !string.IsNullOrEmpty( config.Account ) ) {
			account = config.Account;
		}

		if( string.IsNullOrEmpty( role ) && !string.IsNullOrEmpty( config.Role ) ) {
			role = config.Role;
		}

		if( duration is null or 0 ) {
			if( config.Duration is not ( null or 0 ) ) {
				duration = config.Duration;
			} else {
				duration = 60;
			}
		}

		// if using cache, avoid calling Okta at all if possible
		if( cache && !string.IsNullOrEmpty( account ) && !string.IsNullOrEmpty( role ) ) {
			var cachedCredentials = awsCredentialCache.GetCredentials(
				org: oktaApi.Org,
				user: oktaApi.User,
				accountName: account,
				roleName: role,
				duration: duration.Value
			);

			if( cachedCredentials is not null ) {
				return new(
					Account: account,
					Role: role,
					Duration: duration.Value,
					Credentials: cachedCredentials );
			}
		}

		OktaApp[] awsApps = await oktaApi.Api.GetAwsAccountAppsAsync();

		if( string.IsNullOrEmpty( account ) ) {
			if( nonInteractive ) {
				throw new BmxException( "Account value was not provided" );
			}
			string[] accounts = awsApps.Select( app => app.Label ).ToArray();
			account = consolePrompter.PromptAccount( accounts );
		}

		OktaApp selectedAwsApp = Array.Find(
			awsApps,
			app => app.Label.Equals( account, StringComparison.OrdinalIgnoreCase )
		) ?? throw new BmxException( $"Account {account} could not be found" );

		string loginHtml = await oktaApi.Api.GetPageAsync( selectedAwsApp.LinkUrl );
		string samlResponse = HtmlXmlHelper.GetSamlResponseFromLoginPage( loginHtml );
		AwsRole[] rolesData = HtmlXmlHelper.GetRolesFromSamlResponse( samlResponse );

		if( string.IsNullOrEmpty( role ) ) {
			if( nonInteractive ) {
				throw new BmxException( "Role value was not provided" );
			}
			string[] roles = rolesData.Select( r => r.RoleName ).ToArray();
			role = consolePrompter.PromptRole( roles );
		}

		// try getting from cache again even if calling Okta is inevitable (we still avoid the AWS call)
		if( cache ) {
			var cachedCredentials = awsCredentialCache.GetCredentials(
				org: oktaApi.Org,
				user: oktaApi.User,
				accountName: account,
				roleName: role,
				duration: duration.Value
			);

			if( cachedCredentials is not null ) {
				return new(
					Account: account,
					Role: role,
					Duration: duration.Value,
					Credentials: cachedCredentials );
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

		return new(
			Account: account,
			Role: role,
			Duration: duration.Value,
			Credentials: credentials );
	}
}
