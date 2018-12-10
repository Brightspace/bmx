package serviceProviders

import "github.com/aws/aws-sdk-go/service/sts"

type mockProvider struct {
	getCredentials func(role Role, saml string) (*sts.Credentials, error)
	listRoles      func(encodedSaml string) ([]Role, error)
}

func NewMockProvider() *mockProvider {
	return &mockProvider{}
}

func (m *mockProvider) GetCredentials(role Role, saml string) (*sts.Credentials, error) {
	return m.getCredentials(role, saml)
}

func (m *mockProvider) ListRoles(encodedSaml string) ([]Role, error) {
	return m.listRoles(encodedSaml)
}

func (m *mockProvider) MockGetCredentials(fn func(Role, string) (*sts.Credentials, error)) {
	m.getCredentials = fn
}

func (m *mockProvider) MockListRoles(fn func(encodedSaml string) ([]Role, error)) {
	m.listRoles = fn
}
