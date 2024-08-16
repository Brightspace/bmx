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
	IConsoleWriter consoleWriter,
	IAwsCredentialCache awsCredentialCache,
	BmxConfig config
) {
	public async Task<AwsCredentialsInfo> CreateAwsCredsAsync(
		OktaAuthenticatedContext okta,
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

		var accountSource = ParameterSource.CliArg;
		if( string.IsNullOrEmpty( account ) && !string.IsNullOrEmpty( config.Account ) ) {
			account = config.Account;
			accountSource = ParameterSource.Config;
		}
		if( !string.IsNullOrEmpty( account ) && !nonInteractive ) {
			consoleWriter.WriteParameter( ParameterDescriptions.Account, account, accountSource );
		}

		var roleSource = ParameterSource.CliArg;
		if( string.IsNullOrEmpty( role ) && !string.IsNullOrEmpty( config.Role ) ) {
			role = config.Role;
			roleSource = ParameterSource.Config;
		}
		if( !string.IsNullOrEmpty( role ) && !nonInteractive ) {
			consoleWriter.WriteParameter( ParameterDescriptions.Role, role, roleSource );
		}

		var durationSource = ParameterSource.CliArg;
		if( duration is null or 0 ) {
			if( config.Duration is not ( null or 0 ) ) {
				duration = config.Duration;
				durationSource = ParameterSource.Config;
			} else {
				duration = 60;
				durationSource = ParameterSource.BuiltInDefault;
			}
		}
		if( durationSource != ParameterSource.BuiltInDefault && !nonInteractive ) {
			consoleWriter.WriteParameter( ParameterDescriptions.Duration, duration.Value.ToString(), durationSource );
		}

		// if using cache, avoid calling Okta at all if possible
		if( cache && !string.IsNullOrEmpty( account ) && !string.IsNullOrEmpty( role ) ) {
			var cachedCredentials = awsCredentialCache.GetCredentials(
				org: okta.Org,
				user: okta.User,
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

		OktaApp[] awsApps = await okta.Client.GetAwsAccountAppsAsync();

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

		string loginHtml = await okta.Client.GetPageAsync( selectedAwsApp.LinkUrl );
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
				org: okta.Org,
				user: okta.User,
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
				org: okta.Org,
				user: okta.User,
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
