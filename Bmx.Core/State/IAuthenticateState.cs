namespace Bmx.Core.State
{
	public interface IAuthenticateState {
		MfaOption[] MfaOptions { get; }
	}
}
