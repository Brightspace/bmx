using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Bmx.Core;
using Bmx.Idp.Okta;

namespace Bmx.CommandLine {
	public class CommandLine {
		private BmxCore _bmx;
		private readonly Parser _cmdLineParser;

		public CommandLine() {
			// TODO: Fix this :| (ref: Program.cs)
			_bmx = new BmxCore( new OktaClient( "d2l" ) );
			_cmdLineParser = BuildCommandLine().UseDefaults().Build();

			_bmx.PromptUserName += GetUser;
			_bmx.PromptUserPassword += GetPassword;
			_bmx.PromptMfaType += GetMfaType;
			_bmx.PromptMfaInput += GetMfaInput;
			_bmx.PromptAccountSelection += GetAccount;
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
				new Option<string>( "--account" ) {Description = "account name to auth against"},
				new Option<string>( "--org" ) {Required = true, Description = "okta org api to target"},
				new Option<string>( "--output" ) {
					Required = false, Description = "the output format [bash|powershell]"
				},
				new Option<string>( "--role" ) {Required = false, Description = "desired role to assume"},
				new Option<string>( "--user" ) {Required = false, Description = "user to authenticate with"}
			};
		}

		private Command BuildWriteCommand() {
			return new Command( "write" ) {
				new Option<string>( "--account" ) {Description = "account name to auth against"},
				new Option<string>( "--org" ) {Required = true, Description = "okta org api to target"},
				new Option<string>( "--output" ) {
					Required = false, Description = "write to the specified file instead of ~/.aws/credentials"
				},
				new Option<string>( "--role" ) {Required = false, Description = "desired role to assume"},
				new Option<string>( "--user" ) {Required = false, Description = "user to authenticate with"},
				new Option<string>( "--profile" ) {Required = false, Description = "aws profile name"}
			};
		}

		private void ExecutePrint( string org, string account = null, string role = null, string user = null,
			string output = null ) {
			_bmx.Print( org, account, role, user, output );
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
			return OptionalSelect( mfaOptions );
		}

		private string GetMfaInput( string mfaInputPrompt ) {
			Console.Write( $"{mfaInputPrompt}: " );
			return Console.ReadLine();
		}

		private int GetRoleType( string[] roles ) {
			Console.WriteLine( "Available Roles" );
			return OptionalSelect( roles, "Select a role" );
		}

		private int GetAccount( string[] accounts ) {
			Console.WriteLine( "Available accounts:" );
			return OptionalSelect( accounts, "Select an account" );
		}

		private int OptionalSelect( string[] options, string selectPrompt = "Select an option" ) {
			int selectedOption;

			for( var i = 0; i < options.Length; i++ ) {
				Console.WriteLine( $"[{i}] - {options[i]}" );
			}

			do {
				Console.Write( $"{selectPrompt}: " );
				var inputStr = Console.ReadLine();

				if( inputStr == null || !int.TryParse( inputStr, out selectedOption ) || selectedOption < 0 ||
				    selectedOption >= options.Length ) {
					selectedOption = -1;
					Console.WriteLine( $"Invalid option, valid options are between 0 and {options.Length - 1}" );
				}
			} while( selectedOption < 0 || selectedOption >= options.Length );

			return selectedOption;
		}

		public Task<int> InvokeAsync( string[] args ) {
			return _cmdLineParser.InvokeAsync( args );
		}
	}
}
