using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace D2L.Bmx.Aws;

internal class AwsCredsCache() {
	private static readonly JsonSerializerOptions _options =
		new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

	public void SaveToFile( string? Org, string? User, AwsRole role, AwsCredentials credentials ) {
		if( !Directory.Exists( BmxPaths.BMX_DIR ) ) {
			return;
		}
		if( string.IsNullOrEmpty( Org ) || string.IsNullOrEmpty( User ) ) { //It's not set in config file
			return;
		}
		var newEntry = new AwsCacheModel(
			Org,
			User,
			role.RoleArn,
			credentials );
		List<AwsCacheModel> allEntries = GetAllCache();
		allEntries.RemoveAll( o => o.Credentials.Expiration < DateTime.Now );
		allEntries.Add( newEntry );
		string jsonString = JsonSerializer.Serialize( allEntries, SourceGenerationContext.Default.ListAwsCacheModel );

		WriteTextToFile( BmxPaths.AWS_CREDS_CACHE_FILE_NAME, jsonString );
	}
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

	public List<AwsCacheModel> GetAllCache() {
		string CacheFileName = BmxPaths.AWS_CREDS_CACHE_FILE_NAME;
		if( !File.Exists( CacheFileName ) ) {
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

	public AwsCredentials? GetCachedSession( string? Org, string? User, AwsRole role, bool Cache ) {
		if( string.IsNullOrEmpty( Org ) || string.IsNullOrEmpty( User ) ) { //It's not set in config file
			return null;
		}
		List<AwsCacheModel> allEntries = GetAllCache();
		AwsCacheModel? matchedEntry = allEntries.Find( o => o.User == User && o.Org == Org && o.RoleArn == role.RoleArn
		&& o.Credentials.Expiration > DateTime.Now.AddMinutes( 5 ) );
		if( matchedEntry is not null ) {
			return matchedEntry.Credentials;
		}
		return null;
	}
}
