package identityProviders

import (
	"github.com/Brightspace/bmx/saml/identityProviders/okta"
)

type IdentityProvider interface {
	AuthenticateFromCache(username, org string) (string, bool)
	Authenticate(username, password, org string) (string, error)
	ListApplications(userId string) ([]okta.OktaAppLink, error)
	GetSaml(appLink okta.OktaAppLink) (string, error)
}
