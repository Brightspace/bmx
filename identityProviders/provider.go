package identityProviders

type IdentityProvider interface {
	Authenticate(username, password string) (string, error)
	AuthenticateFromCache(username string) (string, bool, error)
	CompleteMFA(username, url, mfaCode string) (string, error)
	GetMFAFactors() ([]MFAFactor, error)
	GetSaml(app AppDetail) (string, error)
	ListApplications(userID string) ([]AppDetail, error)
}
