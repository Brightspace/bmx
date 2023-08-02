using System.Runtime.InteropServices;
using IniParser;
using IniParser.Model;
namespace D2L.Bmx;

internal interface IBmxConfigProvider {
	BmxConfig GetConfiguration();
	void SaveConfiguration( BmxConfig config );
}

internal class BmxConfigProvider( FileIniDataParser parser ) : IBmxConfigProvider {
	public BmxConfig GetConfiguration() {
		// Main config is at ~/.bmx/config
		string configFileName = BmxPaths.CONFIG_FILE_NAME;
		var data = new IniData();
		if( File.Exists( configFileName ) ) {
			try {
				var tempdata = parser.ReadFile( configFileName );
				data.Merge( tempdata );
			} catch( Exception ) {
				Console.Error.Write( $"WARNING: Failed to load the global config file {configFileName}." );
			}
		}
		// If set, we recursively look up from CWD (all the way to root) for additional bmx config files (labelled as .bmx)
		FileInfo? projectConfigInfo = GetProjectConfigFileInfo();

		if( projectConfigInfo is not null ) {
			try {
				var tempdata = parser.ReadFile( projectConfigInfo.FullName );
				data.Merge( tempdata );
			} catch( Exception ) {
				Console.Error.Write( $"WARNING: Failed to load the local config file {projectConfigInfo.FullName}." );
			}
		}

		int? duration = null;
		if( !string.IsNullOrEmpty( data.Global["duration"] ) ) {
			if( !int.TryParse( data.Global["duration"], out int configDuration ) || configDuration < 1 ) {
				throw new BmxException( "Invalid duration in config" );
			}
			duration = configDuration;
		}

		return new BmxConfig(
			Org: data.Global["org"],
			User: data.Global["user"],
			Account: data.Global["account"],
			Role: data.Global["role"],
			Profile: data.Global["profile"],
			Duration: duration
		);
	}

	public void SaveConfiguration( BmxConfig config ) {
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
		using var fs = new FileStream( BmxPaths.CONFIG_FILE_NAME, op );
		using var reader = new StreamReader( fs );
		var data = parser.ReadData( reader );

		if( !string.IsNullOrEmpty( config.Org ) ) {
			data.Global["org"] = config.Org;
		}
		if( !string.IsNullOrEmpty( config.User ) ) {
			data.Global["user"] = config.User;
		}
		if( config.Duration.HasValue ) {
			data.Global["duration"] = $"{config.Duration}";
		}

		fs.Position = 0;
		fs.SetLength( 0 );

		using var writer = new StreamWriter( fs );
		parser.WriteData( writer, data );
	}

	// Look from cwd up the directory chain all the way to root for a .bmx file
	private static FileInfo? GetProjectConfigFileInfo() {
		try {
			DirectoryInfo? currentDirectory = new( Directory.GetCurrentDirectory() );

			// limit the maximum search depth to avoid infinite loop even if there's any symlink trickery
			for( int i = 0; i < 2000 && currentDirectory is not null; i++ ) {
				var bmxConfigFiles = currentDirectory.GetFiles( ".bmx" );

				if( bmxConfigFiles.Length > 1 ) {
					// TODO: Log this
					return null;
				}

				if( bmxConfigFiles.Length == 1 ) {
					return bmxConfigFiles[0];
				}

				currentDirectory = currentDirectory.Parent;
			}

			return null;
		} catch( Exception ) {
			// if there's any error finding a .bmx file, assume it doesn't exist rather than crash the app
			// TODO: debug log?
			return null;
		}
	}
}
