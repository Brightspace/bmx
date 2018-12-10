package identityProviders

type mockProvider struct {
	authenticate          func(username, password string) (string, error)
	authenticateFromCache func(username string) (string, bool, error)
	completeMFA           func(username, url, mfaCode string) (string, error)
	getMFAFactors         func() ([]MFAFactor, error)
	getSaml               func(app AppDetail) (string, error)
	listApplications      func(userID string) ([]AppDetail, error)
}

func NewMockProvider() *mockProvider {
	return &mockProvider{}
}

func (m *mockProvider) Authenticate(username, password string) (string, error) {
	return m.authenticate(username, password)
}

func (m *mockProvider) AuthenticateFromCache(username string) (string, bool, error) {
	return m.authenticateFromCache(username)
}

func (m *mockProvider) CompleteMFA(username, url, mfaCode string) (string, error) {
	return m.completeMFA(username, url, mfaCode)
}

func (m *mockProvider) GetMFAFactors() ([]MFAFactor, error) {
	return m.getMFAFactors()
}

func (m *mockProvider) GetSaml(app AppDetail) (string, error) {
	return m.getSaml(app)
}

func (m *mockProvider) ListApplications(userID string) ([]AppDetail, error) {
	return m.listApplications(userID)
}

func (m *mockProvider) MockAuthenticate(fn func(string, string) (string, error)) {
	m.authenticate = fn
}

func (m *mockProvider) MockAuthenticateFromCache(fn func(string) (string, bool, error)) {
	m.authenticateFromCache = fn
}

func (m *mockProvider) MockCompleteMFA(fn func(string, string, string) (string, error)) {
	m.completeMFA = fn
}

func (m *mockProvider) MockGetMFAFactors(fn func() ([]MFAFactor, error)) {
	m.getMFAFactors = fn
}

func (m *mockProvider) MockGetSaml(fn func(AppDetail) (string, error)) {
	m.getSaml = fn
}

func (m *mockProvider) MockListApplications(fn func(string) ([]AppDetail, error)) {
	m.listApplications = fn
}
