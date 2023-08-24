using IniParser.Model;
namespace D2L.Bmx.Aws;

internal class AwsCredsCache() {
	public void SaveToFile( AwsRole role, var authResp ) {
		if( !Directory.Exists( BmxPaths.BMX_DIR ) ) {
			Directory.CreateDirectory( BmxPaths.BMX_DIR );
		}
		var op = new FileStreamOptions {
			Mode = FileMode.OpenOrCreate,
			Access = FileAccess.ReadWrite,
		};
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var fs = new FileStream( BmxPaths.AWS_CREDS_CACHE_FILE_NAME, op );
		using var reader = new StreamReader( fs );
		var data = parser.ReadData( reader );
		if( !string.IsNullOrEmpty( authResp.Credentials.SessionToken ) ) {
			data[role.RoleArn]["SessionToken"] = authResp.Credentials.SessionToken;
		}
		if( !string.IsNullOrEmpty( authResp.Credentials.AccessKeyId ) ) {
			data[role.RoleArn]["AccessKeyId"] = authResp.Credentials.AccessKeyId;
		}
		if( !string.IsNullOrEmpty( authResp.Credentials.SecretAccessKey ) ) {
			data[role.RoleArn]["SecretAccessKey"] = authResp.Credentials.SecretAccessKey;
		}
		if( !string.IsNullOrEmpty( authResp.Credentials.SessionToken ) ) {
			data[role.RoleArn]["Expiration"] = authResp.Credentials.Expiration.ToUniversalTime();
		}

		fs.Position = 0;
		fs.SetLength( 0 );

		using var writer = new StreamWriter( fs );
		parser.WriteData( writer, data );
	}
	public AwsCredentials GetCache( AwsRole role ) {
		// Main config is at ~/.bmx/config
		string CacheFileName = BmxPaths.AWS_CREDS_CACHE_FILE_NAME;
		var data = new IniData();
		if( File.Exists( CacheFileName ) ) {
			try {
				var tempdata = parser.ReadFile( CacheFileName );
				data.Merge( tempdata );
			} catch( Exception ) {
				Console.Error.Write( $"WARNING: Failed to load the saved Creds file {configFileName}." );
			}
		}
		if( data[role.RoleArn] is not null ) {
			if( DateTime.Parse( data[role.RoleArn]["Expiration"] ) < DateTime.Now() ) {
				return new AwsCredentials(
					SessionToken: data[role.RoleArn]["SessionToken"],
					AccessKeyId: data[role.RoleArn]["AccessKeyId"],
					SecretAccessKey: data[role.RoleArn]["SecretAccessKey"],
					Expiration: data[role.RoleArn]["Expiration"]
				);
			}
		}

		return null;
	}
}
