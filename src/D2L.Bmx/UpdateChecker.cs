using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.IO;
using System.Runtime.InteropServices;

namespace D2L.Bmx {
	internal class UpdateChecker {

		public static async Task CheckForUpdatesAsync( BmxConfig config ) {
			try {
				var savedVersion = GetSavedLatestVersion();
				var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
				var latestVersion = new Version( savedVersion ?? "0.0.0" );
				if( ShouldFetchLatestVersion( savedVersion ) ) {
					latestVersion = new Version( await GetLatestReleaseVersionAsync() );
				}

				var updateLocation = ( string.Equals( config.Org, "d2l", StringComparison.OrdinalIgnoreCase ) )
					? "https://bmx.d2l.dev"
					: "https://github.com/Brightspace/bmx/releases/latest";

				if( latestVersion > localVersion ) {
					DisplayUpdateMessage( $"A new BMX release is available: v{latestVersion} (current: v{localVersion})\n" +
						$"Upgrade now at {updateLocation}" );
				}
			} catch( Exception ex ) {
				// Give up and don't bother telling the user we couldn't check for updates
				Console.Error.WriteLine( ex.ToString() );
			}
		}

		private static void DisplayUpdateMessage( string message ) {
			var originalBackgroundColor = Console.BackgroundColor;
			var originalForegroundColor = Console.ForegroundColor;

			Console.BackgroundColor = ConsoleColor.Gray;
			Console.ForegroundColor = ConsoleColor.Black;

			var lines = message.Split( "\n" );
			int consoleWidth = Console.WindowWidth;

			foreach( var line in lines ) {
				Console.Error.Write( line.PadRight( consoleWidth ) );
				Console.Error.WriteLine();
			}

			Console.BackgroundColor = originalBackgroundColor;
			Console.ForegroundColor = originalForegroundColor;
			Console.ResetColor();
			Console.Error.WriteLine();
		}

		private static async Task<string> GetLatestReleaseVersionAsync() {
			using var httpClient = new HttpClient {
				BaseAddress = new Uri( "https://api.github.com" ),
				Timeout = TimeSpan.FromSeconds( 5 )
			};
			httpClient.DefaultRequestHeaders.Add( "User-Agent", "BMX" );
			var response = await httpClient.GetAsync( "repos/Brightspace/bmx/releases/latest" );
			response.EnsureSuccessStatusCode();

			using var responseStream = await response.Content.ReadAsStreamAsync();
			var releaseData = await JsonSerializer.DeserializeAsync(
				responseStream,
				SourceGenerationContext.Default.GithubRelease
			);
			string content = await response.Content.ReadAsStringAsync();
			string version = releaseData?.TagName?.Replace( "v", "" ) ?? string.Empty;
			SaveLatestVersion( version );
			return version;
		}

		private static void SaveLatestVersion( string version ) {
			if( string.IsNullOrWhiteSpace( version ) ) {
				return;
			}
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
			using FileStream fs = new FileStream( BmxPaths.UPDATE_CHECK_FILE_NAME, op );
			using StreamWriter writer = new StreamWriter( fs );
			writer.WriteLine( version );
		}

		private static DateTimeOffset? GetTimeLastChecked() {
			if( !File.Exists( BmxPaths.UPDATE_CHECK_FILE_NAME ) ) {
				return null;
			}
			return File.GetLastWriteTimeUtc( BmxPaths.UPDATE_CHECK_FILE_NAME );
		}

		private static bool ShouldFetchLatestVersion( string? savedVersion ) {
			if( string.IsNullOrWhiteSpace( savedVersion ) ) {
				return true;
			}
			var savedTimestamp = GetTimeLastChecked();
			if( !savedTimestamp.HasValue || ( DateTimeOffset.UtcNow - savedTimestamp.Value ) > TimeSpan.FromDays( 1 ) ) {
				return true;
			}
			return false;
		}

		private static string? GetSavedLatestVersion() {
			if( !File.Exists( BmxPaths.UPDATE_CHECK_FILE_NAME ) ) {
				return null;
			}
			return File.ReadAllText( BmxPaths.UPDATE_CHECK_FILE_NAME );
		}
	}

	internal record GithubRelease {
		[JsonPropertyName( "tag_name" )]
		public string? TagName { get; set; }
	}
}
