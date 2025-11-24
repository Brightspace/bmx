using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Amazon.Runtime;
using Amazon.SecurityToken;
using D2L.Bmx;
using D2L.Bmx.Aws;
using D2L.Bmx.GitHub;
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
var loginCommand = new Command( "login", "Log into Okta and save an Okta session" ) {
	orgOption,
	userOption,
};
loginCommand.SetHandler( ( InvocationContext context ) => {
	var messageWriter = new MessageWriter();
	var config = new BmxConfigProvider( new FileIniDataParser(), messageWriter ).GetConfiguration();
	var handler = new LoginHandler( new OktaAuthenticator(
		new OktaClientFactory(),
		new OktaSessionStorage(),
		new BrowserLauncher(),
		new ConsolePrompter(),
		messageWriter,
		config
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
		new BmxConfigProvider( new FileIniDataParser(), new MessageWriter() ),
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
var cacheAwsCredentialsOption = new Option<bool>(
	name: "--cache",
	description: ParameterDescriptions.CacheAwsCredentials );

var printCommand = new Command( "print", "Print AWS credentials" ) {
	accountOption,
	roleOption,
	durationOption,
	formatOption,
	orgOption,
	userOption,
	nonInteractiveOption,
	cacheAwsCredentialsOption,
};

printCommand.SetHandler( ( InvocationContext context ) => {
	var consolePrompter = new ConsolePrompter();
	var messageWriter = new MessageWriter();
	var config = new BmxConfigProvider( new FileIniDataParser(), messageWriter ).GetConfiguration();
	var handler = new PrintHandler(
		new OktaAuthenticator(
			new OktaClientFactory(),
			new OktaSessionStorage(),
			new BrowserLauncher(),
			consolePrompter,
			messageWriter,
			config ),
		new AwsCredsCreator(
			new AwsClient( new AmazonSecurityTokenServiceClient( new AnonymousAWSCredentials() ) ),
			consolePrompter,
			messageWriter,
			new AwsCredsCache(),
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
		cacheAwsCredentials: context.ParseResult.GetValueForOption( cacheAwsCredentialsOption )
	);
} );

// bmx write
var outputOption = new Option<string>(
	name: "--output",
	description: ParameterDescriptions.Output );
var profileOption = new Option<string>(
	name: "--profile",
	description: ParameterDescriptions.Profile );
var useCredentialProcessOption = new Option<bool>(
	name: "--use-credential-process",
	description: ParameterDescriptions.UseCredentialProcess );

var writeCommand = new Command( "write", "Write AWS credentials to the credentials file" ) {
	accountOption,
	roleOption,
	profileOption,
	durationOption,
	outputOption,
	orgOption,
	userOption,
	nonInteractiveOption,
	cacheAwsCredentialsOption,
	useCredentialProcessOption,
};

writeCommand.SetHandler( ( InvocationContext context ) => {
	var consolePrompter = new ConsolePrompter();
	var messageWriter = new MessageWriter();
	var config = new BmxConfigProvider( new FileIniDataParser(), messageWriter ).GetConfiguration();
	var handler = new WriteHandler(
		new OktaAuthenticator(
			new OktaClientFactory(),
			new OktaSessionStorage(),
			new BrowserLauncher(),
			consolePrompter,
			messageWriter,
			config ),
		new AwsCredsCreator(
			new AwsClient( new AmazonSecurityTokenServiceClient( new AnonymousAWSCredentials() ) ),
			consolePrompter,
			messageWriter,
			new AwsCredsCache(),
			config ),
		consolePrompter,
		messageWriter,
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
		cacheAwsCredentials: context.ParseResult.GetValueForOption( cacheAwsCredentialsOption ),
		useCredentialProcess: context.ParseResult.GetValueForOption( useCredentialProcessOption )
	);
} );

var updateCommand = new Command( "update", "Updates BMX to the latest version" );
updateCommand.SetHandler( ( InvocationContext context ) => {
	var handler = new UpdateHandler( new GitHubClient(), new VersionProvider() );
	return handler.HandleAsync();
} );

// root command
var rootCommand = new RootCommand( "BMX grants you API access to your AWS accounts!" ) {
	// put more frequently used commands first, as the order here affects help text
	printCommand,
	writeCommand,
	loginCommand,
	configureCommand,
	updateCommand,
};

// start bmx
return await new CommandLineBuilder( rootCommand )
	.UseDefaults()
	.AddMiddleware(
		middleware: async ( context, next ) => {
			// initialize BMX directories
			if( !Directory.Exists( BmxPaths.CACHE_DIR ) ) {
				try {
					// the cache directory is inside the config directory (~/.bmx), so this ensures both exist
					Directory.CreateDirectory( BmxPaths.CACHE_DIR );
					// move the old sessions file (v3.0-) into the new cache directory (v3.1+)
					if( File.Exists( BmxPaths.SESSIONS_FILE_LEGACY_NAME ) ) {
						File.Move( BmxPaths.SESSIONS_FILE_LEGACY_NAME, BmxPaths.SESSIONS_FILE_NAME );
					}
				} catch( Exception ex ) {
					throw new BmxException( "Failed to initialize BMX directory (~/.bmx)", ex );
				}
			}

			if( context.ParseResult.CommandResult.Command != updateCommand ) {
				var updateChecker = new UpdateChecker( new GitHubClient(), new VersionProvider(), new MessageWriter() );
				await updateChecker.CheckForUpdatesAsync();
			}

			await next( context );
		},
		// The default order for new middleware is after the middleware for `--help` & `--version` and can get short-circuited.
		// We want our middleware (especially update checks) to almost always run, even on `--help` & `--version`,
		// so we specify a custom order just after the exception handler (which is way before `--help` & `--version`).
		order: MiddlewareOrder.ExceptionHandler + 1
	)
	.UseExceptionHandler( ( exception, context ) => {
		IMessageWriter messageWriter = new MessageWriter();
		if( exception is BmxException ) {
			messageWriter.WriteError( exception.Message );
		} else {
			messageWriter.WriteError( "BMX exited with unexpected internal error" );
		}
		if( BmxEnvironment.IsDebug ) {
			messageWriter.WriteError( $"[DEBUG] Exception: {exception}" );
		}
	} )
	.Build()
	.InvokeAsync( args );
