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

		bool.TryParse( configBuilder.Build()["allow_project_configs"], out bool shouldReadProjectConfig );

		FileInfo? projectConfigInfo = null;

		if( shouldReadProjectConfig ) {
			projectConfigInfo = GetProjectConfigFileInfo();
		}

		if( shouldReadProjectConfig && projectConfigInfo is not null ) {
			// Default file provider ignores files prefixed with ".", we need to provide our own as a result
			var fileProvider =
				new PhysicalFileProvider( projectConfigInfo.DirectoryName!, ExclusionFilters.None );
			configBuilder.AddIniFile( fileProvider, projectConfigInfo.Name, optional: false,
				reloadOnChange: false );
		}

		var config = configBuilder.Build();

		int? defaultDuration = null;
		if( !string.IsNullOrEmpty( config["default_duration"] ) ) {
			if( ( !int.TryParse( config["default_duration"], out int configDuration ) || configDuration < 1 ) ) {
				throw new BmxException( "Invalid duration in config" );
			}
			defaultDuration = configDuration;
		}

		return new BmxConfig(
			Org: config["org"],
			User: config["user"],
			Account: config["account"],
			Role: config["role"],
			Profile: config["profile"],
			DefaultDuration: defaultDuration,
			AllowProjectConfigs: shouldReadProjectConfig
		);
	}

	public void SaveConfiguration( BmxConfig config ) {

		string bmxDirectory = BmxPaths.BMX_DIR;
		string configFileName = BmxPaths.CONFIG_FILE_NAME;

		if( !Directory.Exists( bmxDirectory ) ) {
			Directory.CreateDirectory( bmxDirectory );
		}

		if( !File.Exists( configFileName ) ) {
			File.WriteAllText( configFileName, "" );
		}

		using var writer = new StreamWriter( BmxPaths.CONFIG_FILE_NAME, append: false );

		if( !string.IsNullOrEmpty( config.Org ) ) {
			writer.WriteLine( $"org={config.Org}" );
		}
		if( !string.IsNullOrEmpty( config.User ) ) {
			writer.WriteLine( $"user={config.User}" );
		}
		if( config.DefaultDuration.HasValue ) {
			writer.WriteLine( $"default_duration={config.DefaultDuration}" );
		}
		if( config.AllowProjectConfigs ) {
			writer.WriteLine( $"allow_project_configs={config.AllowProjectConfigs}" );
		}
	}

	// Look from cwd up the directory chain all the way to root for a .bmx file
	private static FileInfo? GetProjectConfigFileInfo() {
		DirectoryInfo? currentDirectory = new( Directory.GetCurrentDirectory() );

		while( currentDirectory is not null ) {
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
	}
}
