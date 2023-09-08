using System.Runtime.InteropServices;
using System.Text.Json;
namespace D2L.Bmx.Aws;

internal interface IAwsCredentialCache {
	public AwsCredentials? GetCredentials( string org, string user, AwsRole role, int duration );
	public void SetCredentials( string org, string user, AwsRole role, AwsCredentials credentials );
}

internal class AwsCredsCache : IAwsCredentialCache {
	private static void WriteTextToFile( string path, string content ) {
		var op = new FileStreamOptions {
			Mode = FileMode.Create,
			Access = FileAccess.Write,
		};
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
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
			return JsonSerializer.Deserialize( sessionsJson, SourceGenerationContext.Default.ListAwsCacheModel )
				?? new();
		} catch( JsonException ) {
			return new();
		}
	}

	void IAwsCredentialCache.SetCredentials( string org, string user, AwsRole role, AwsCredentials credentials ) {
		List<AwsCacheModel> allEntries = GetAllCache();
		allEntries.RemoveAll( o =>
			o.Credentials.Expiration < DateTime.Now
			|| o.User != user
			|| o.Org != org
		);
		allEntries.Add( new AwsCacheModel(
			Org: org,
			User: user,
			RoleArn: role.RoleArn,
			Credentials: credentials
		) );
		string jsonString = JsonSerializer.Serialize( allEntries, SourceGenerationContext.Default.ListAwsCacheModel );

		WriteTextToFile( BmxPaths.AWS_CREDS_CACHE_FILE_NAME, jsonString );
	}

	AwsCredentials? IAwsCredentialCache.GetCredentials( string org, string user, AwsRole role, int duration ) {
		List<AwsCacheModel> allEntries = GetAllCache();

		// Too stale, within a window (of 10 mins) if requested duration was <= 20 mins, 15 mins otherwise
		// Avoids over-eagerly invalidating cache when requesting AWS credentials for short durations
		var ttlLongerThan = DateTime.Now.AddMinutes( duration > 20 ? 15 : duration - 5 );
		AwsCacheModel? matchedEntry = allEntries.Find(
			o => o.Org == org
			&& o.User == user
			&& o.RoleArn == role.RoleArn
			&& o.Credentials.Expiration >= ttlLongerThan
		);

		return matchedEntry?.Credentials;
	}
}
