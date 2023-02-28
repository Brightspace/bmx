using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
namespace D2L.Bmx;

internal interface IBmxConfigProvider {
	BmxConfig GetConfiguration();
}

internal class BmxConfigProvider : IBmxConfigProvider {

	public BmxConfig GetConfiguration() {
		// Main config is at ~/.bmx/config
		var configLocation = Path.Join( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".bmx",
			"config" );

		// If set, we recursively look up from CWD (all the way to root) for additional bmx config files (labelled as .bmx)
		var configBuilder = new ConfigurationBuilder().AddIniFile( configLocation, optional: true );

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
			DefaultDuration: defaultDuration
		);
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
