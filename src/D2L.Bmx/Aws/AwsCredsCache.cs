using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace D2L.Bmx.Aws;

internal class AwsCredsCache() {
	private static readonly JsonSerializerOptions _options =
		new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

	[RequiresUnreferencedCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	[RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)" )]
	public void SaveToFile( string Org, string User, AwsRole role, AwsCredentials credentials ) {
		if( !Directory.Exists( BmxPaths.BMX_DIR ) ) {
			return;
		}
		if( Org == "" || User == "" ) { //It's not set in config file
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
		string jsonString = JsonSerializer.Serialize( allEntries );

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

	[RequiresUnreferencedCode
	( "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)" )]
	[RequiresDynamicCode( "Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)" )]
	public List<AwsCacheModel> GetAllCache() {
		string CacheFileName = BmxPaths.AWS_CREDS_CACHE_FILE_NAME;
		if( !File.Exists( CacheFileName ) ) {
			return new();
		}
		try {
			string sessionsJson = File.ReadAllText( BmxPaths.AWS_CREDS_CACHE_FILE_NAME );
			return JsonSerializer.Deserialize<List<AwsCacheModel>>( sessionsJson )
				?? new();
		} catch( JsonException ) {
			return new();
		}
	}

	[RequiresUnreferencedCode( "Calls D2L.Bmx.Aws.AwsCredsCache.GetAllCache()" )]
	[RequiresDynamicCode( "Calls D2L.Bmx.Aws.AwsCredsCache.GetAllCache()" )]
	public AwsCredentials? GetCachedSession( string Org, string User, AwsRole role, int Cache ) {
		// Main config is at ~/.bmx/config
		List<AwsCacheModel> allEntries = GetAllCache();
		AwsCacheModel? matchedEntry = allEntries.Find( o => o.User == User && o.Org == Org && o.RoleArn == role.RoleArn
		&& o.Credentials.Expiration > DateTime.Now.AddMinutes( Cache ) );
		if( matchedEntry is not null ) {
			return matchedEntry.Credentials;
		}
		return null;
	}
}
