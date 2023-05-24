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
	description: "the tenant name or full domain name of the Okta organization" );
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
var nonInteractiveOption = new Option<bool>(
	name: "--non-interactive",
	getDefaultValue: () => false,
	description: "If the print handler should be run without user input and assume all information is provided" );
var printOutputOption = new Option<string>(
	name: "--output",
	description: "the output format [bash|powershell|json]"
	);
printOutputOption.AddValidator( result => {
	string? output = result.GetValueForOption( printOutputOption );
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

var configureCommand = new Command( "configure", "Create a bmx config file to save Okta sessions" ) {
	orgOption,
	userOption,
	durationOption,
};

configureCommand.SetHandler( ( InvocationContext context ) => {
	var handler = new ConfigureHandler(
		new BmxConfigProvider(),
		new ConsolePrompter() );
	try {
		handler.Handle(
			org: context.ParseResult.GetValueForOption( orgOption ),
			user: context.ParseResult.GetValueForOption( userOption ),
			defaultDuration: context.ParseResult.GetValueForOption( durationOption )
		);
	} catch( BmxException e ) {
		Console.Error.WriteLine( e.Message );
		context.ExitCode = 1;
	}
} );

rootCommand.Add( configureCommand );

var printCommand = new Command( "print", "Returns the AWS credentials in text as environment variables / json" ) {
	accountOption,
	durationOption,
	orgOption,
	printOutputOption,
	roleOption,
	userOption,
	nonInteractiveOption,
};

printCommand.SetHandler( async ( InvocationContext context ) => {
	var consolePrompter = new ConsolePrompter();
	var config = new BmxConfigProvider().GetConfiguration();
	var handler = new PrintHandler(
		new OktaAuthenticator(
			new OktaApi(),
			new OktaSessionStorage(),
			consolePrompter,
			config ),
		new AwsCredsCreator(
			new AwsClient( new AmazonSecurityTokenServiceClient( new AnonymousAWSCredentials() ) ),
			consolePrompter,
			config )
	);
	try {
		await handler.HandleAsync(
			org: context.ParseResult.GetValueForOption( orgOption ),
			user: context.ParseResult.GetValueForOption( userOption ),
			account: context.ParseResult.GetValueForOption( accountOption ),
			role: context.ParseResult.GetValueForOption( roleOption ),
			duration: context.ParseResult.GetValueForOption( durationOption ),
			nonInteractive: context.ParseResult.GetValueForOption( nonInteractiveOption ),
			output: context.ParseResult.GetValueForOption( printOutputOption )
		);
	} catch( BmxException e ) {
		Console.Error.WriteLine( e.Message );
		context.ExitCode = 1;
	}
} );

rootCommand.Add( printCommand );

var writeCommand = new Command( "write", "Write to AWS credentials file" ) {
	accountOption,
	durationOption,
	orgOption,
	writeOutputOption,
	profileOption,
	roleOption,
	userOption,
	nonInteractiveOption,
};

writeCommand.SetHandler( async ( InvocationContext context ) => {
	var consolePrompter = new ConsolePrompter();
	var config = new BmxConfigProvider().GetConfiguration();
	var handler = new WriteHandler(
		new OktaAuthenticator(
			new OktaApi(),
			new OktaSessionStorage(),
			consolePrompter,
			config ),
		new AwsCredsCreator(
			new AwsClient( new AmazonSecurityTokenServiceClient( new AnonymousAWSCredentials() ) ),
			consolePrompter,
			config ),
		consolePrompter,
		config
	);
	try {
		await handler.HandleAsync(
			org: context.ParseResult.GetValueForOption( orgOption ),
			user: context.ParseResult.GetValueForOption( userOption ),
			account: context.ParseResult.GetValueForOption( accountOption ),
			role: context.ParseResult.GetValueForOption( roleOption ),
			duration: context.ParseResult.GetValueForOption( durationOption ),
			nonInteractive: context.ParseResult.GetValueForOption( nonInteractiveOption ),
			output: context.ParseResult.GetValueForOption( writeOutputOption ),
			profile: context.ParseResult.GetValueForOption( profileOption )
		);
	} catch( BmxException e ) {
		Console.Error.WriteLine( e.Message );
		context.ExitCode = 1;
	}
} );

rootCommand.Add( writeCommand );

return await rootCommand.InvokeAsync( args );
