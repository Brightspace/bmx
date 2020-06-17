using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Bmx.Core;

namespace Bmx.CommandLine {
	public class CommandLine {
		private BmxCore _bmx;
		private Parser _cmdLineParser;

		public CommandLine() {
			_bmx = new BmxCore();
			_cmdLineParser = BuildCommandLine().UseDefaults().Build();
		}

		private CommandLineBuilder BuildCommandLine() {
			var rootCommand = new RootCommand( "BMX (github.com/Brightspace/bmx), procure AWS credentials" );

			var printCommand = BuildPrintCommand();
			var writeCommand = BuildWriteCommand();

			rootCommand.Add( printCommand );
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

		public Task<int> InvokeAsync( string[] args ) {
			return _cmdLineParser.InvokeAsync( args );
		}
	}
}
