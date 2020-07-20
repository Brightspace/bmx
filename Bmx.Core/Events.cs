namespace Bmx.Core {
	public delegate string PromptUserNameHandler( string identityProvider );

	public delegate string PromptUserPasswordHandler( string identityProvider );

	public delegate int PromptMfaTypeHandler( string[] mfaOptions );

	public delegate string PromptMfaInputHandler( string mfaInputPrompt );

	public delegate int PromptAccountSelectHandler( string[] accounts );

	public delegate int PromptRoleSelectionHandler( string[] roles );

	public delegate void InformUnknownMfaTypesHandler( string[] mfaOptions );
}
