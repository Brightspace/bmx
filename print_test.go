package bmx_test

import (
	"net/url"
	"testing"
	"time"

	"github.com/aws/aws-sdk-go/aws"

	"github.com/aws/aws-sdk-go/service/sts"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/saml/identityProviders/okta"
)

type mokta struct {
	BaseUrl *url.URL
}

func (m *mokta) Authenticate(username string, password string) (string, error) {
	return "1", nil
}

func (m *mokta) ListApplications(userId string) ([]okta.OktaAppLink, error) {
	response := []okta.OktaAppLink{
		{Id: "id", Label: "label", LinkUrl: "url", AppName: "appname", AppInstanceId: "instanceid"},
	}

	return response, nil
}

func (m *mokta) GetSaml(appLink okta.OktaAppLink) (string, error) {
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

type awsServiceProviderMock struct{}

func (a *awsServiceProviderMock) GetCredentials(saml string) *sts.Credentials {
	return &sts.Credentials{
		AccessKeyId:     aws.String("access_key_id"),
		SecretAccessKey: aws.String("secrest_access_key"),
		SessionToken:    aws.String("session_token"),
		Expiration:      aws.Time(time.Now().Add(time.Hour * 8)),
	}
}

func TestMonkey(t *testing.T) {
	options := bmx.PrintCmdOptions{
		Org: "myorg",
	}

	oktaClient := &mokta{}

	bmx.AwsServiceProvider = &awsServiceProviderMock{}
	bmx.Print(oktaClient, options)
}
