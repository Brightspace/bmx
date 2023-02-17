using System.CommandLine;

var orgOption = new Option<string?>(
	name: "--org",
	description: "the okta org api to target" );
var userOption = new Option<string?>(
	name: "--user",
	description: "the user to authenticate with" );
var accountOption = new Option<string?>(
	name: "--account",
	description: "the account name to auth against" );
var roleOption = new Option<string?>(
	name: "--role",
	description: "the desired role to assume" );
var durationOption = new Option<int?>(
	name: "--duration",
	description: "duration of session in minutes",
	getDefaultValue: () => 60 );
var nomaskOption = new Option<bool>(
	name: "--nomask",
	description: "set to not mask the password, helps with debugging",
	getDefaultValue: () => false );
var printOutputOption = new Option<string?>(
	name: "--output",
	description: "the output format [bash|powershell]" );
var writeOutputOption = new Option<string?>(
	name: "--output",
	description: "write to the specified file instead of ~/.aws/credentials" );
var profileOption = new Option<string?>(
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

printCommand.SetHandler(
	Print.PrintHandler,
	orgOption, userOption, accountOption, roleOption, durationOption, nomaskOption, printOutputOption
 );

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

writeCommand.SetHandler(
	Write.WriteHandler,
	orgOption, userOption, accountOption, roleOption, durationOption, nomaskOption, writeOutputOption, profileOption
 );

rootCommand.Add( writeCommand );

// version and help commands are not implemented because they can be called as flags (--version and --help respectively)
return await rootCommand.InvokeAsync( args );
