package bmx_test

import (
	"fmt"
	"net/http"
	"net/url"
	"strconv"
	"testing"
	"time"

	"github.com/aws/aws-sdk-go/service/sts/stsiface"

	"github.com/aws/aws-sdk-go/aws"

	"github.com/aws/aws-sdk-go/service/sts"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/okta"
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

type queuedConsoleReader struct {
	ResponseItems []string
	currentItem   int
}

func (q *queuedConsoleReader) ReadLine(prompt string) (string, error) {
	r := q.ResponseItems[q.currentItem]
	q.currentItem++
	fmt.Printf("%s: %s\n", prompt, r)
	return r, nil
}
func (q *queuedConsoleReader) ReadPassword(prompt string) (string, error) {
	r := q.ResponseItems[q.currentItem]
	q.currentItem++
	fmt.Printf("%s: %s\n", prompt, r)
	return r, nil
}
func (q *queuedConsoleReader) ReadInt(prompt string) (int, error) {
	r, _ := strconv.Atoi(q.ResponseItems[q.currentItem])
	q.currentItem++
	fmt.Printf("%s: %d\n", prompt, r)
	return r, nil
}

type stsMock struct {
	stsiface.STSAPI
}

func (s *stsMock) AssumeRoleWithSAML(input *sts.AssumeRoleWithSAMLInput) (*sts.AssumeRoleWithSAMLOutput, error) {
	out := &sts.AssumeRoleWithSAMLOutput{
		Credentials: &sts.Credentials{
			AccessKeyId:     aws.String("access_key_id"),
			SecretAccessKey: aws.String("secrest_access_key"),
			SessionToken:    aws.String("session_token"),
			Expiration:      aws.Time(time.Now().Add(time.Hour * 8)),
		},
	}

	return out, nil
}

func TestMonkey(t *testing.T) {
	cr := &queuedConsoleReader{
		ResponseItems: []string{
			"myuser",
			"mypassword",
			"0",
		},
	}
	options := bmx.PrintCmdOptions{
		Org:           "myorg",
		ConsoleReader: cr,
		StsClient:     &stsMock{},
	}

	oktaClient := &mokta{}

	bmx.Print(oktaClient, options)
}
