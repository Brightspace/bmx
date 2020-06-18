using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Bmx.Core;

namespace Bmx.CommandLine {
	class StubIdp : IIdentityProvider {
		public string Name => "StubIDP";

		public Task<MfaOption[]> Authenticate( string username, string password ) {
			throw new NotImplementedException();
		}

		public Task<bool> ChallengeMfa( int selectedMfaIndex, string challengeResponse ) {
			throw new NotImplementedException();
		}
	}

	public class CommandLine {
		private BmxCore _bmx;
		private readonly Parser _cmdLineParser;

		public CommandLine() {
			_bmx = new BmxCore( new StubIdp() );
			_cmdLineParser = BuildCommandLine().UseDefaults().Build();

			_bmx.PromptUserName += GetUser;
			_bmx.PromptUserPassword += GetPassword;
			_bmx.PromptMfaType += GetMfaType;
			_bmx.PromptMfaInput += GetMfaInput;
			_bmx.PromptRoleSelection += GetRoleType;
		}

		private CommandLineBuilder BuildCommandLine() {
			var rootCommand = new RootCommand( "BMX (github.com/Brightspace/bmx), procure AWS credentials" );

			var printCommand = BuildPrintCommand();
			var writeCommand = BuildWriteCommand();

			rootCommand.Add( printCommand );
			printCommand.Handler = CommandHandler.Create<string, string, string, string, string>( ExecutePrint );

			rootCommand.Add( writeCommand );

			return new CommandLineBuilder( rootCommand );
		}

		private Command BuildPrintCommand() {
			return new Command( "print", "Prints credentials to screen" ) {
				// TODO: Consider generating these elsewhere, overlaps with write command options
				new Option<string>( "--account" ) {Required = true, Description = "account name to auth against"},
				new Option<string>( "--org" ) {Required = true, Description = "okta org api to target"},
				new Option<string>( "--output" ) {Required = false, Description = "the output format [bash|powershell]"},
				new Option<string>( "--role" ) {Required = false, Description = "desired role to assume"},
				new Option<string>( "--user" ) {Required = false, Description = "user to authenticate with"}
			};
		}

		private Command BuildWriteCommand() {
			return new Command( "write" ) {
				new Option<string>( "--account" ) {Required = true, Description = "account name to auth against"},
				new Option<string>( "--org" ) {Required = true, Description = "okta org api to target"},
				new Option<string>( "--output" ) {Required = false, Description = "write to the specified file instead of ~/.aws/credentials"},
				new Option<string>( "--role" ) {Required = false, Description = "desired role to assume"},
				new Option<string>( "--user" ) {Required = false, Description = "user to authenticate with"},
				new Option<string>( "--profile" ) {Required = false, Description = "aws profile name"}
			};
		}

		private void ExecutePrint( string account, string org, string role = null, string user = null,
			string output = null ) {
			_bmx.Print( account, org, role, user, output );
		}

		private string GetUser( string provider ) {
			Console.Write( $"{provider} Username: " );
			return Console.ReadLine();
		}

		private string GetPassword( string provider ) {
			Console.Write( $"{provider} Password: " );

			string pw = "";
			ConsoleKeyInfo key;

			// Empty key masked password entry, based off of:
			// https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring?view=netcore-3.1#examples
			do {
				key = Console.ReadKey( true );

				if( key.Key == ConsoleKey.Backspace && pw.Length > 0 ) {
					pw = pw.Remove( pw.Length - 1 );
				} else if( key.Key != ConsoleKey.Enter ) {
					pw += key.KeyChar;
					Console.SetCursorPosition( Console.CursorLeft - 1, Console.CursorTop );
					Console.Write( " " );
				}
			} while( key.Key != ConsoleKey.Enter );

			Console.WriteLine();
			return pw;
		}

		private int GetMfaType( string[] mfaOptions ) {
			Console.WriteLine( "MFA Required" );

			for( var i = 0; i < mfaOptions.Length; i++ ) {
				Console.WriteLine( $"[{i}] - {mfaOptions[i]}" );
			}

			int selectedMfaOption;

			do {
				Console.Write( "Select an available MFA Option: " );
				var inputStr = Console.ReadLine();

				if( inputStr == null || !int.TryParse( inputStr, out selectedMfaOption ) || selectedMfaOption < 0 ||
				    selectedMfaOption >= mfaOptions.Length ) {
					selectedMfaOption = -1;
					Console.WriteLine( $"Invalid option, valid options are between 0 and {mfaOptions.Length - 1}" );
				}
			} while( selectedMfaOption < 0 || selectedMfaOption >= mfaOptions.Length );

			return selectedMfaOption;
		}

		private string GetMfaInput( string mfaInputPrompt ) {
			Console.Write( $"{mfaInputPrompt}: " );
			return Console.ReadLine();
		}

		private string GetRoleType( string[] roles ) {
			Console.WriteLine( "Available Roles" );

			for( var i = 0; i < roles.Length; i++ ) {
				Console.WriteLine( $"[{i}] - {roles[i]}" );
			}

			int selectedRole;

			do {
				Console.Write( "Select a role: " );
				var inputStr = Console.ReadLine();

				if( inputStr == null || !int.TryParse( inputStr, out selectedRole ) || selectedRole < 0 ||
				    selectedRole >= roles.Length ) {
					selectedRole = -1;
					Console.WriteLine( $"Invalid option, valid options are between 0 and {roles.Length - 1}" );
				}
			} while( selectedRole < 0 || selectedRole >= roles.Length );

			return roles[selectedRole];
		}

		public Task<int> InvokeAsync( string[] args ) {
			return _cmdLineParser.InvokeAsync( args );
		}
	}
}
