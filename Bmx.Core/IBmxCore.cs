namespace Bmx.Core
{
	public interface IBmxCore
	{
		event PromptUserNameHandler PromptUserName;
		event PromptUserPasswordHandler PromptUserPassword;
		event PromptMfaTypeHandler PromptMfaType;
		event PromptMfaInputHandler PromptMfaInput;
		event PromptAccountSelectHandler PromptAccountSelection;
		event PromptRoleSelectionHandler PromptRoleSelection;
		event PromptRoleSelectionHandler InformUnknownMfaTypesHandler;

		void Print( string org, string account = null, string role = null, string user = null,
			string output = "powershell" );
	}
}
