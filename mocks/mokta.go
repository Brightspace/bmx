package mocks

import (
	"net/url"

	"github.com/Brightspace/bmx/identityProviders"
)

type Mokta struct {
	BaseUrl *url.URL
}

func (m *Mokta) Authenticate(username, password, org string) (string, error) {
	return "1", nil
}

func (m *Mokta) ListApplications(userId string) ([]identityProviders.AppDetail, error) {
	response := []identityProviders.AppDetail{
		{ID: "id", Label: "label", LinkURL: "url", AppName: "appname", AppInstanceID: "instanceid"},
	}

	return response, nil
}

func (m *Mokta) AuthenticateFromCache(username, org string) (string, bool) {
	return "", false
}

func (m *Mokta) GetSaml(app identityProviders.AppDetail) (string, error) {
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
