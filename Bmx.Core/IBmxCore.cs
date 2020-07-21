using Bmx.Core.State;

namespace Bmx.Core {
	public interface IBmxCore<TAuthenticateState, TAuthenticatedState, TAccountState, TRoleState>
		where TAuthenticateState : IAuthenticateState
		where TAuthenticatedState : IAuthenticatedState
		where TAccountState : IAccountState
		where TRoleState : IRoleState {
		event PromptUserNameHandler PromptUserName;
		event PromptUserPasswordHandler PromptUserPassword;
		event PromptMfaTypeHandler PromptMfaType;
		event PromptMfaInputHandler PromptMfaInput;
		event PromptAccountSelectHandler PromptAccountSelection;
		event PromptRoleSelectionHandler PromptRoleSelection;
		event InformUnknownMfaTypesHandler InformUnknownMfaTypes;

		void Print( string org, string account = null, string role = null, string user = null,
			string output = "powershell" );
	}
}
