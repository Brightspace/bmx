package bmx_test

import (
	"net/http"
	"net/url"
	"testing"
	"time"

	"github.com/Brightspace/bmx/saml/identityProviders"

	"github.com/aws/aws-sdk-go/aws"

	"github.com/aws/aws-sdk-go/service/sts"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/okta"
	"github.com/Brightspace/bmx/saml/serviceProviders"
)

type mokta struct {
	BaseUrl *url.URL
}

func (m *mokta) GetBaseUrl() *url.URL {
	return m.BaseUrl
}

func (m *mokta) GetHttpClient() *http.Client {
	return nil
}

func (m *mokta) Authenticate(username string, password string) (*okta.OktaAuthResponse, error) {
	response := &okta.OktaAuthResponse{}
	return response, nil
}

func (m *mokta) StartSession(sessionToken string) (*okta.OktaSessionResponse, error) {
	response := &okta.OktaSessionResponse{}
	return response, nil
}

func (m *mokta) SetSessionId(id string) {

}

func (m *mokta) ListApplications(userId string) ([]okta.OktaAppLink, error) {
	response := []okta.OktaAppLink{
		{Id: "id", Label: "label", LinkUrl: "url", AppName: "appname", AppInstanceId: "instanceid"},
	}

	return response, nil
}

func (m *mokta) GetSaml(appLink okta.OktaAppLink) (okta.Saml2pResponse, string, error) {
	saml := okta.Saml2pResponse{
		Assertion: okta.Saml2Assertion{
			AttributeStatement: okta.Saml2AttributeStatement{
				Attributes: []okta.Saml2Attribute{
					{Name: "https://aws.amazon.com/SAML/Attributes/Role", Value: "principal_arn,role_arn"},
				},
			},
		},
	}
	raw := ""

	return saml, raw, nil
}

type awsServiceProviderMock struct{}

func (a *awsServiceProviderMock) GetCredentials(oktaClient identityProviders.IdentityProvider, user serviceProviders.UserInfo) *sts.Credentials {
	return &sts.Credentials{
		AccessKeyId:     aws.String("access_key_id"),
		SecretAccessKey: aws.String("secrest_access_key"),
		SessionToken:    aws.String("session_token"),
		Expiration:      aws.Time(time.Now().Add(time.Hour * 8)),
	}
}

func TestMonkey(t *testing.T) {
	options := bmx.PrintCmdOptions{
		Org:      "myorg",
		Provider: &awsServiceProviderMock{},
	}

	oktaClient := &mokta{}

	bmx.Print(oktaClient, options)
}
