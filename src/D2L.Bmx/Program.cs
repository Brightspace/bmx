using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Amazon.Runtime;
using Amazon.SecurityToken;
using D2L.Bmx;
using D2L.Bmx.Aws;
using D2L.Bmx.Okta;

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
var durationOption = new Option<int?>(
	name: "--duration",
	description: "duration of session in minutes" );
durationOption.AddValidator( result => {
	if( result.GetValueForOption( durationOption ) < 1 ) {
		result.ErrorMessage = "Invalid duration";
	}
} );
var headlessOption = new Option<bool>(
	name: "--headless",
	getDefaultValue: () => false,
	description: "If the print handler should be run headless and assume all information is provided" );
var nomaskOption = new Option<bool>(
	name: "--nomask",
	getDefaultValue: () => false,
	description: "set to not mask the password, helps with debugging" );
var printOutputOption = new Option<string>(
	name: "--output",
	description: "the output format [bash|powershell|json]"
	);
printOutputOption.AddValidator( result => {
	var output = result.GetValueForOption( printOutputOption );
	var supportedOutputs = new HashSet<string>( StringComparer.OrdinalIgnoreCase ) {
		"bash", "powershell", "json"
	};

	if( !string.IsNullOrEmpty( output ) && !supportedOutputs.Contains( output ) ) {
		result.ErrorMessage = "Unsupported output option. Please select from Bash or PowerShell";
	}
} );
var writeOutputOption = new Option<string>(
	name: "--output",
	description: "write to the specified file path instead of ~/.aws/credentials" );
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
		headlessOption
	};

printCommand.SetHandler( async ( InvocationContext context ) => {
	var handler = new PrintHandler(
		new BmxConfigProvider(),
		new OktaApi(),
		new AwsClient( new AmazonSecurityTokenServiceClient( new AnonymousAWSCredentials() ) ) );
	try {
		await handler.HandleAsync(
			org: context.ParseResult.GetValueForOption( orgOption ),
			user: context.ParseResult.GetValueForOption( userOption ),
			account: context.ParseResult.GetValueForOption( accountOption ),
			role: context.ParseResult.GetValueForOption( roleOption ),
			duration: context.ParseResult.GetValueForOption( durationOption ),
			nomask: context.ParseResult.GetValueForOption( nomaskOption ),
			headless: context.ParseResult.GetValueForOption( headlessOption ),
			output: context.ParseResult.GetValueForOption( printOutputOption )
		);
	} catch( BmxException e ) {
		Console.Error.WriteLine( e.Message );
		context.ExitCode = 1;
	}
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

writeCommand.SetHandler( async ( InvocationContext context ) => {
	var handler = new WriteHandler(
		new BmxConfigProvider(),
		new OktaApi(),
		new AwsClient( new AmazonSecurityTokenServiceClient( new AnonymousAWSCredentials() ) ) );
	try {
		await handler.HandleAsync(
			org: context.ParseResult.GetValueForOption( orgOption ),
			user: context.ParseResult.GetValueForOption( userOption ),
			account: context.ParseResult.GetValueForOption( accountOption ),
			role: context.ParseResult.GetValueForOption( roleOption ),
			duration: context.ParseResult.GetValueForOption( durationOption ),
			nomask: context.ParseResult.GetValueForOption( nomaskOption ),
			output: context.ParseResult.GetValueForOption( writeOutputOption ),
			profile: context.ParseResult.GetValueForOption( profileOption )
		);
	} catch( BmxException e ) {
		Console.Error.WriteLine( e.Message );
		context.ExitCode = 1;
	}
} );

rootCommand.Add( writeCommand );

// version and help commands are not implemented. they can be called as flags (--version and --help respectively). However they can be added later if necessary
return await rootCommand.InvokeAsync( args );
