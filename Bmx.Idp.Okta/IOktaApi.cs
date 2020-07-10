using System;
using System.Threading.Tasks;
using Bmx.Idp.Okta.Models;

namespace Bmx.Idp.Okta {
	public interface IOktaApi {
		void SetOrganization( string organization );
		void AddSession( string sessionId );
		Task<AuthenticateResponseInital> AuthenticateOkta( AuthenticateOptions authOptions );
		Task<AuthenticateResponseSuccess> AuthenticateChallengeMfaOkta( AuthenticateChallengeMfaOptions authOptions );
		Task<OktaSession> CreateSessionOkta( SessionOptions sessionOptions );
		Task<OktaApp[]> GetAccountsOkta( string userId );
		Task<string> GetAccountOkta( Uri linkUri );
	}
}
