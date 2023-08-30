using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Amazon.Runtime;
using Amazon.SecurityToken;
using D2L.Bmx;
using D2L.Bmx.Aws;
using D2L.Bmx.Okta;
using IniParser;

// common options
var orgOption = new Option<string>(
	name: "--org",
	description: ParameterDescriptions.Org );
var userOption = new Option<string>(
	name: "--user",
	description: ParameterDescriptions.User );

// bmx login
var loginCommand = new Command( "login", "Log into Okta and save an Okta session" ){
	orgOption,
	userOption,
};
loginCommand.SetHandler( ( InvocationContext context ) => {
	var handler = new LoginHandler( new OktaAuthenticator(
		new OktaApi(),
		new OktaSessionStorage(),
		new ConsolePrompter(),
		new BmxConfigProvider( new FileIniDataParser() ).GetConfiguration()
	) );
	return handler.HandleAsync(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption )
	);
} );

// configure & print & write common options
var durationOption = new Option<int?>(
	name: "--duration",
	description: ParameterDescriptions.Duration );

durationOption.AddValidator( result => {
	if( !(
		result.Tokens is [Token token, ..]
		&& int.TryParse( token.Value, out int duration )
		&& duration >= 15 && duration <= 720
	) ) {
		result.ErrorMessage = "Duration must be an integer between 15 and 720";
	}
} );

var nonInteractiveOption = new Option<bool>(
	name: "--non-interactive",
	description: ParameterDescriptions.NonInteractive );

// bmx configure
var configureCommand = new Command( "configure", "Create or update the global BMX config file" ) {
	orgOption,
	userOption,
	durationOption,
	nonInteractiveOption,
};

configureCommand.SetHandler( ( InvocationContext context ) => {
	var handler = new ConfigureHandler(
		new BmxConfigProvider( new FileIniDataParser() ),
		new ConsolePrompter() );
	handler.Handle(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		duration: context.ParseResult.GetValueForOption( durationOption ),
		nonInteractive: context.ParseResult.GetValueForOption( nonInteractiveOption )
	);
	return Task.CompletedTask;
} );

// print & write common options
var accountOption = new Option<string>(
	name: "--account",
	description: ParameterDescriptions.Account );
var roleOption = new Option<string>(
	name: "--role",
	description: ParameterDescriptions.Role );

// bmx print
var formatOption = new Option<string>(
	name: "--format",
	description: ParameterDescriptions.Format );
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
		result.ErrorMessage = $"Unsupported value for --output. Must be one of:\n{string.Join( '\n', PrintFormat.All )}";
	}
} );
var cacheOption = new Option<int?>(
	name: "--use-cache",
	description: ParameterDescriptions.UseCacheTimeLimit );

var printCommand = new Command( "print", "Print AWS credentials" ) {
	accountOption,
	roleOption,
	durationOption,
	formatOption,
	orgOption,
	userOption,
	nonInteractiveOption,
	cacheOption,
};

printCommand.SetHandler( ( InvocationContext context ) => {
	var consolePrompter = new ConsolePrompter();
	var config = new BmxConfigProvider( new FileIniDataParser() ).GetConfiguration();
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
		format: context.ParseResult.GetValueForOption( formatOption ),
		useCacheTimeLimit: context.ParseResult.GetValueForOption( cacheOption )
	);
} );

// bmx write
var outputOption = new Option<string>(
	name: "--output",
	description: ParameterDescriptions.Output );
var profileOption = new Option<string>(
	name: "--profile",
	description: ParameterDescriptions.Profile );

var writeCommand = new Command( "write", "Write AWS credentials to the credentials file" ) {
	accountOption,
	roleOption,
	profileOption,
	durationOption,
	outputOption,
	orgOption,
	userOption,
	nonInteractiveOption,
	cacheOption,
};

writeCommand.SetHandler( ( InvocationContext context ) => {
	var consolePrompter = new ConsolePrompter();
	var config = new BmxConfigProvider( new FileIniDataParser() ).GetConfiguration();
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
		config,
		new FileIniDataParser()
	);
	return handler.HandleAsync(
		org: context.ParseResult.GetValueForOption( orgOption ),
		user: context.ParseResult.GetValueForOption( userOption ),
		account: context.ParseResult.GetValueForOption( accountOption ),
		role: context.ParseResult.GetValueForOption( roleOption ),
		duration: context.ParseResult.GetValueForOption( durationOption ),
		nonInteractive: context.ParseResult.GetValueForOption( nonInteractiveOption ),
		output: context.ParseResult.GetValueForOption( outputOption ),
		profile: context.ParseResult.GetValueForOption( profileOption ),
		useCacheTimeLimit: context.ParseResult.GetValueForOption( cacheOption )
	);
} );

// root command
var rootCommand = new RootCommand( "BMX grants you API access to your AWS accounts!" ) {
	// put more frequently used commands first, as the order here affects help text
	printCommand,
	writeCommand,
	loginCommand,
	configureCommand,
};

// start bmx
return await new CommandLineBuilder( rootCommand )
	.UseDefaults()
	.UseExceptionHandler( ( exception, context ) => {
		Console.ResetColor();
		Console.ForegroundColor = ConsoleColor.Red;
		if( exception is BmxException ) {
			Console.Error.WriteLine( exception.Message );
		} else {
			Console.Error.WriteLine( "BMX exited with unexpected internal error" );
		}
		if( Environment.GetEnvironmentVariable( "BMX_DEBUG" ) == "1" ) {
			Console.Error.WriteLine( exception );
		}
		Console.ResetColor();
	} )
	.Build()
	.InvokeAsync( args );
