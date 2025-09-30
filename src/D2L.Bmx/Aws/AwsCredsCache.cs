using System.Text.Json;

namespace D2L.Bmx.Aws;

internal interface IAwsCredentialCache {
	AwsCredentials? GetCredentials( string org, string user, string accountName, string roleName, int duration );
	void SetCredentials( string org, string user, string accountName, string roleName, AwsCredentials credentials );
}

internal class AwsCredsCache : IAwsCredentialCache {
	private static void WriteTextToFile( string path, string content ) {
		var op = new FileStreamOptions {
			Mode = FileMode.Create,
			Access = FileAccess.Write,
		};
		if( !OperatingSystem.IsWindows() ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var writer = new StreamWriter( path, op );
		writer.Write( content );
	}

	private List<AwsCacheModel> GetAllCache() {
		if( !File.Exists( BmxPaths.AWS_CREDS_CACHE_FILE_NAME ) ) {
			return new();
		}
		try {
			string sessionsJson = File.ReadAllText( BmxPaths.AWS_CREDS_CACHE_FILE_NAME );
			return JsonSerializer.Deserialize( sessionsJson, JsonCamelCaseContext.Default.ListAwsCacheModel )
				?? new();
		} catch( JsonException ) {
			return new();
		}
	}

	void IAwsCredentialCache.SetCredentials(
		string org,
		string user,
		string accountName,
		string roleName,
		AwsCredentials credentials
	) {
		List<AwsCacheModel> allEntries = GetAllCache();

		allEntries.Add( new AwsCacheModel(
			Org: org,
			User: user,
			AccountName: accountName,
			RoleName: roleName,
			Credentials: credentials
		) );

		var minimumExpiryTime = DateTime.UtcNow.AddMinutes( 10 );

		var prunedEntries = allEntries
			// Remove expired/expiring & entries not related to current user
			.Where( o =>
				o.Credentials.Expiration >= minimumExpiryTime
				&& o.User == user
				&& o.Org == org
			)
			// Prune older (closer to expiry) credentials for the same role
			.GroupBy( o => (o.AccountName, o.RoleName), ( _, entries ) =>
				entries.OrderBy( o => o.Credentials.Expiration )
			)
			.Select( o => o.Last() );

		string jsonString = JsonSerializer.Serialize( prunedEntries.ToList(),
			JsonCamelCaseContext.Default.ListAwsCacheModel );

		WriteTextToFile( BmxPaths.AWS_CREDS_CACHE_FILE_NAME, jsonString );
	}

	AwsCredentials? IAwsCredentialCache.GetCredentials(
		string org,
		string user,
		string accountName,
		string roleName,
		int duration
	) {
		List<AwsCacheModel> allEntries = GetAllCache();

		// Too stale, within a window (of 10 mins) if requested duration was <= 20 mins, 15 mins otherwise
		// Avoids over-eagerly invalidating cache when requesting AWS credentials for short durations
		var minimumExpiryTime = DateTime.UtcNow.AddMinutes( duration > 20 ? 15 : duration - 5 );
		var matchedEntry = allEntries.Find(
			o => o.Org == org
			&& o.User == user
			&& o.AccountName.Equals( accountName, StringComparison.OrdinalIgnoreCase )
			&& o.RoleName.Equals( roleName, StringComparison.OrdinalIgnoreCase )
			&& o.Credentials.Expiration >= minimumExpiryTime
		);

		return matchedEntry?.Credentials;
	}
}
