using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace D2L.Bmx;

internal interface IBmxConfigProvider {
	BmxConfig GetConfiguration();
	void SaveConfiguration( BmxConfig config );
}

internal class BmxConfigProvider : IBmxConfigProvider {
	public BmxConfig GetConfiguration() {
		// Main config is at ~/.bmx/config
		string configFileName = BmxPaths.CONFIG_FILE_NAME;

		// If set, we recursively look up from CWD (all the way to root) for additional bmx config files (labelled as .bmx)
		var configBuilder = new ConfigurationBuilder().AddIniFile( configFileName, optional: true );

		FileInfo? projectConfigInfo = GetProjectConfigFileInfo();

		if( projectConfigInfo is not null ) {
			// Default file provider ignores files prefixed with ".", we need to provide our own as a result
			var fileProvider =
				new PhysicalFileProvider( projectConfigInfo.DirectoryName!, ExclusionFilters.None );
			configBuilder.AddIniFile( fileProvider, projectConfigInfo.Name, optional: false,
				reloadOnChange: false );
		}

		var config = configBuilder.Build();

		int? duration = null;
		if( !string.IsNullOrEmpty( config["duration"] ) ) {
			if( !int.TryParse( config["duration"], out int configDuration ) || configDuration < 1 ) {
				throw new BmxException( "Invalid duration in config" );
			}
			duration = configDuration;
		}

		return new BmxConfig(
			Org: config["org"],
			User: config["user"],
			Account: config["account"],
			Role: config["role"],
			Profile: config["profile"],
			Duration: duration
		);
	}

	public void SaveConfiguration( BmxConfig config ) {
		if( !Directory.Exists( BmxPaths.BMX_DIR ) ) {
			Directory.CreateDirectory( BmxPaths.BMX_DIR );
		}
		var op = new FileStreamOptions {
			Mode = FileMode.Create,
			Access = FileAccess.Write,
		};
		if( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
			op.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		}
		using var writer = new StreamWriter( BmxPaths.CONFIG_FILE_NAME, op );

		if( !string.IsNullOrEmpty( config.Org ) ) {
			writer.WriteLine( $"org={config.Org}" );
		}
		if( !string.IsNullOrEmpty( config.User ) ) {
			writer.WriteLine( $"user={config.User}" );
		}
		if( config.Duration.HasValue ) {
			writer.WriteLine( $"duration={config.Duration}" );
		}
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
