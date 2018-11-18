package identityProviders

import (
	"github.com/Brightspace/bmx/saml/identityProviders/okta"
)

type IdentityProvider interface {
	Authenticate(username string, password string) (string, error)
	ListApplications(userId string) ([]okta.OktaAppLink, error)
	GetSaml(appLink okta.OktaAppLink) (string, error)
}
