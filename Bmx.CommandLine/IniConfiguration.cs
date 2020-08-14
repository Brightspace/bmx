#nullable enable
using System;
using System.IO;
using System.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Bmx.CommandLine {
	public class IniConfiguration : IBmxConfig {
		public string? Org { get; }
		public string? User { get; }
		public string? Account { get; }
		public string? Role { get; }
		public string? Profile { get; }

		public IniConfiguration() {
			var config = GetConfiguration();

			Org = config["org"];
			User = config["user"];
			Account = config["account"];
			Role = config["role"];
			Profile = config["profile"];
		}

		private static IConfiguration GetConfiguration() {
			// Main config is at ~/.bmx/config
			var configLocation = Path.Join( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".bmx",
				"config" );

			// If set, we recursively look up from CWD (all the way to root) for additional bmx config files (labelled as .bmx)
			var config = new ConfigurationBuilder().AddIniFile( configLocation, optional: true );

			bool.TryParse( config.Build()["allow_project_configs"], out bool shouldReadProjectConfig );

			string? projectConfigPath = null;

			if( shouldReadProjectConfig ) {
				projectConfigPath = GetProjectConfigPath();
			}

			if( shouldReadProjectConfig && projectConfigPath != null ) {
				// Default file provider ignores files prefixed with ".", we need to provide our own as a result
				var fileProvider =
					new PhysicalFileProvider( Path.GetDirectoryName( projectConfigPath ), ExclusionFilters.None );
				config.AddIniFile( fileProvider, Path.GetFileName( projectConfigPath ), optional: false,
					reloadOnChange: false );
			}

			return config.Build();
		}

		// Look from cwd up the directory chain all the way to root for a .bmx file
		private static string? GetProjectConfigPath() {
			DirectoryInfo? currentDirectory = new DirectoryInfo( Directory.GetCurrentDirectory() );

			while( currentDirectory != null ) {
				try {
					var bmxConfigFiles = currentDirectory.GetFiles( ".bmx" );

					if( bmxConfigFiles.Length > 1 ) {
						// TODO: Log this
						return null;
					}

					if( bmxConfigFiles.Length == 1 ) {
						return bmxConfigFiles[0].FullName;
					}

					currentDirectory = currentDirectory.Parent;
				} catch( SecurityException ) {
					// TODO: Log this
					return null;
				}
			}

			return null;
		}
	}
}
