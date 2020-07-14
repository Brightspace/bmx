#nullable enable
using System;
using System.IO;
using System.Security;
using Microsoft.Extensions.Configuration;

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
			var configLocation =
				Path.Join( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".bmx", "config" );

			// If set, we recursively look up from CWD (all the way to root) for additional bmx config files (labelled as .bmx)
			var shouldReadProjectConfig = ShouldReadProjectConfigs( configLocation );
			string? projectConfigPath = null;

			if( shouldReadProjectConfig ) {
				projectConfigPath = GetProjectConfigPath();
			}

			var config = new ConfigurationBuilder().AddIniFile( configLocation );

			if( shouldReadProjectConfig && projectConfigPath != null ) {
				config.AddIniFile( projectConfigPath );
			}

			return config.Build();
		}

		private static bool ShouldReadProjectConfigs( string primaryConfigLocation ) {
			var config = new ConfigurationBuilder()
				.AddIniFile( primaryConfigLocation ).Build();
			bool.TryParse( config["allow_project_configs"], out bool allowProjectConfigs );
			return allowProjectConfigs;
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
