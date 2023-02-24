using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using D2L.Bmx;

var orgOption = new Option<string>(
	name: "--org",
	description: "the okta org api to target" );
var userOption = new Option<string>(
	name: "--user",
	description: "the user to authenticate with" );
var accountOption = new Option<string>(
	name: "--account",
	description: "the account name to auth against" );
var roleOption = new Option<string>(
	name: "--role",
	description: "the desired role to assume" );
var durationOption = new Option<int>(
	name: "--duration",
	getDefaultValue: () => 60,
	description: "duration of session in minutes" );
var nomaskOption = new Option<bool>(
	name: "--nomask",
	getDefaultValue: () => false,
	description: "set to not mask the password, helps with debugging" );
var printOutputOption = new Option<string>(
	name: "--output",
	description: "the output format [bash|powershell]" );
var writeOutputOption = new Option<string>(
	name: "--output",
	description: "write to the specified file instead of ~/.aws/credentials" );
var profileOption = new Option<string>(
	name: "--profile",
	description: "aws profile name" );

var rootCommand = new RootCommand();

var printCommand = new Command( "print", "Print the long stuff to screen" )
	{
		accountOption,
		durationOption,
		nomaskOption,
		orgOption,
		printOutputOption,
		roleOption,
		userOption,
	};

printCommand.SetHandler( ( InvocationContext context ) => {
	var handler = new PrintHandler( new ConsoleWrapper() );
	handler.Handle(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		account: context.ParseResult.GetValueForOption( accountOption ),
		role: context.ParseResult.GetValueForOption( roleOption ),
		duration: context.ParseResult.GetValueForOption( durationOption ),
		nomask: context.ParseResult.GetValueForOption( nomaskOption ),
		output: context.ParseResult.GetValueForOption( writeOutputOption )
	);
} );

rootCommand.Add( printCommand );

var writeCommand = new Command( "write", "Write to aws credential file" )
	{
		accountOption,
		durationOption,
		nomaskOption,
		orgOption,
		writeOutputOption,
		profileOption,
		roleOption,
		userOption,
	};

writeCommand.SetHandler( ( InvocationContext context ) => {
	var handler = new WriteHandler( new ConsoleWrapper() );
	handler.Handle(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		account: context.ParseResult.GetValueForOption( accountOption ),
		role: context.ParseResult.GetValueForOption( roleOption ),
		duration: context.ParseResult.GetValueForOption( durationOption ),
		nomask: context.ParseResult.GetValueForOption( nomaskOption ),
		output: context.ParseResult.GetValueForOption( writeOutputOption ),
		profile: context.ParseResult.GetValueForOption( profileOption )
	);
} );

rootCommand.Add( writeCommand );

// version and help commands are not implemented. they can be called as flags (--version and --help respectively). However they can be added later if necessary
return await rootCommand.InvokeAsync( args );
