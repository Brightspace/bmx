using D2L.Bmx.Aws;
using D2L.Bmx.Okta;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

internal class AwsCredsCreator(
	IAwsClient awsClient,
	IConsolePrompter consolePrompter,
	BmxConfig config
) {
	public async Task<AwsCredentials> CreateAwsCredsAsync(
		IOktaApi oktaApi,
		string? account,
		string? role,
		int? duration,
		bool nonInteractive,
		bool Cache
	) {
		//cache this one?
		OktaApp[] awsApps = await oktaApi.GetAwsAccountAppsAsync();

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

		string loginHtml = await oktaApi.GetPageAsync( selectedAwsApp.LinkUrl );
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

		return await awsClient.GetTokensAsync(
			samlResponse,
			selectedRoleData,
			duration.Value,
			Cache,
			config.Org,
			config.User );
	}
}
