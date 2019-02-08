package mocks

import (
	"net/url"

	"github.com/Brightspace/bmx/saml/identityProviders/okta"
)

type Mokta struct {
	BaseUrl *url.URL
}

func (m *Mokta) Authenticate(username, password, org string) (string, error) {
	return "1", nil
}

func (m *Mokta) ListApplications(userId string) ([]okta.OktaAppLink, error) {
	response := []okta.OktaAppLink{
		{Id: "id", Label: "label", LinkUrl: "url", AppName: "appname", AppInstanceId: "instanceid"},
	}

	return response, nil
}

func (m *Mokta) AuthenticateFromCache(username, org string) (string, bool) {
	return "", false
}

func (m *Mokta) GetSaml(appLink okta.OktaAppLink) (string, error) {
	// saml := okta.Saml2pResponse{
	// 	Assertion: okta.Saml2Assertion{
	// 		AttributeStatement: okta.Saml2AttributeStatement{
	// 			Attributes: []okta.Saml2Attribute{
	// 				{Name: "https://aws.amazon.com/SAML/Attributes/Role", Value: "principal_arn,role_arn"},
	// 			},
	// 		},
	// 	},
	// }
	raw := ""

	return raw, nil
}
