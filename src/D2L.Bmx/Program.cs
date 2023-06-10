using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Amazon.Runtime;
using Amazon.SecurityToken;
using D2L.Bmx;
using D2L.Bmx.Aws;
using D2L.Bmx.Okta;

var rootCommand = new RootCommand( "BMX grants you API access to your AWS accounts!" );

// common options
var orgOption = new Option<string>(
	name: "--org",
	description: "the tenant name or full domain name of the Okta organization" );
var userOption = new Option<string>(
	name: "--user",
	description: "the user to authenticate with" );
var durationOption = new Option<int?>(
	name: "--duration",
	description: "duration of session in minutes" );
durationOption.AddValidator( result => {
	if( result.GetValueForOption( durationOption ) < 15 ) {
		result.ErrorMessage = "duration must be at least 15";
	}
} );

// bmx configure
var configureCommand = new Command( "configure", "Create a bmx config file to save Okta sessions" ) {
	orgOption,
	userOption,
	durationOption,
};

configureCommand.SetHandler( ( InvocationContext context ) => RunWithErrorHandlingAsync( context, () => {
	var handler = new ConfigureHandler(
		new BmxConfigProvider(),
		new ConsolePrompter() );
	handler.Handle(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		duration: context.ParseResult.GetValueForOption( durationOption )
	);
	return Task.CompletedTask;
} ) );

rootCommand.Add( configureCommand );

// print & write common options
var accountOption = new Option<string>(
	name: "--account",
	description: "the account name to auth against" );
var roleOption = new Option<string>(
	name: "--role",
	description: "the desired role to assume" );
var nonInteractiveOption = new Option<bool>(
	name: "--non-interactive",
	description: "Run non-interactively without showing any prompts." );

// bmx print
var formatOption = new Option<string>(
	name: "--format",
	getDefaultValue: () => OperatingSystem.IsWindows() ? PrintFormat.PowerShell : PrintFormat.Bash,
	description: "the output format" );
/* Intentionally not using:
	- an enum, because .NET will happily parse any integer as an enum (even when outside of defined values) and
	System.CommandLine shows internal enum type name to users when there's a parsing error;
	- `FromAmong`, because it doesn't support case insensitive matching. This may change in future versions of
	System.CommandLine.
*/
formatOption.AddCompletions( _ => PrintFormat.All );
formatOption.AddValidator( result => {
	if( !(
		result.GetValueForOption( formatOption ) is string format
		&& PrintFormat.All.Contains( format )
	) ) {
		result.ErrorMessage = $"Unsupported value for --format. Must be one of:\n{string.Join( '\n', PrintFormat.All )}";
	}
} );

var printCommand = new Command( "print", "Returns the AWS credentials in text as environment variables / json" ) {
	accountOption,
	durationOption,
	orgOption,
	formatOption,
	roleOption,
	userOption,
	nonInteractiveOption,
};

printCommand.SetHandler( ( InvocationContext context ) => RunWithErrorHandlingAsync( context, () => {
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
	return handler.HandleAsync(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		account: context.ParseResult.GetValueForOption( accountOption ),
		role: context.ParseResult.GetValueForOption( roleOption ),
		duration: context.ParseResult.GetValueForOption( durationOption ),
		nonInteractive: context.ParseResult.GetValueForOption( nonInteractiveOption ),
		format: context.ParseResult.GetValueForOption( formatOption )
	);
} ) );

rootCommand.Add( printCommand );

// bmx write
var outputOption = new Option<string>(
	name: "--output",
	description: "write to the specified file path instead of ~/.aws/credentials" );
var profileOption = new Option<string>(
	name: "--profile",
	description: "aws profile name" );

var writeCommand = new Command( "write", "Write to AWS credentials file" ) {
	accountOption,
	durationOption,
	orgOption,
	outputOption,
	profileOption,
	roleOption,
	userOption,
	nonInteractiveOption,
};

writeCommand.SetHandler( ( InvocationContext context ) => RunWithErrorHandlingAsync( context, () => {
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
	return handler.HandleAsync(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		account: context.ParseResult.GetValueForOption( accountOption ),
		role: context.ParseResult.GetValueForOption( roleOption ),
		duration: context.ParseResult.GetValueForOption( durationOption ),
		nonInteractive: context.ParseResult.GetValueForOption( nonInteractiveOption ),
		output: context.ParseResult.GetValueForOption( outputOption ),
		profile: context.ParseResult.GetValueForOption( profileOption )
	);
} ) );

rootCommand.Add( writeCommand );

// start bmx
return await rootCommand.InvokeAsync( args );

// helper functions
static async Task RunWithErrorHandlingAsync( InvocationContext context, Func<Task> handle ) {
	try {
		await handle();
	} catch( Exception e ) {
		Console.ResetColor();
		Console.ForegroundColor = ConsoleColor.Red;
		if( e is BmxException ) {
			Console.Error.WriteLine( e.Message );
		} else {
			Console.Error.WriteLine( "BMX exited with unexpected internal error" );
		}
		if( Environment.GetEnvironmentVariable( "BMX_DEBUG" ) == "1" ) {
			Console.Error.WriteLine( e );
		}
		Console.ResetColor();
		context.ExitCode = 1;
	}
}
