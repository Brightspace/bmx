/*
Copyright 2019 D2L Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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
