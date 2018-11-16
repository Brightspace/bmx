package identityProviders

import (
	"net/http"
	"net/url"

	"github.com/Brightspace/bmx/okta"
)

type IdentityProvider interface {
	Authenticate(username string, password string) (*okta.OktaAuthResponse, error)
	GetHttpClient() *http.Client
	GetBaseUrl() *url.URL
	StartSession(sessionToken string) (*okta.OktaSessionResponse, error)
	ListApplications(userId string) ([]okta.OktaAppLink, error)
	SetSessionId(id string)
	GetSaml(appLink okta.OktaAppLink) (okta.Saml2pResponse, string, error)
}
