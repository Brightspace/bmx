package aws

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"
	"strings"

	"github.com/Brightspace/bmx/console"
	"github.com/Brightspace/bmx/okta"
	"github.com/Brightspace/bmx/saml/identityProviders"
	"github.com/Brightspace/bmx/saml/serviceProviders"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/sts"
)

const (
	usernamePrompt = "Okta Username: "
	passwordPrompt = "Okta Password: "
)

type AwsServiceProvider struct{}

func (a AwsServiceProvider) GetCredentials(oktaClient identityProviders.IdentityProvider, user serviceProviders.UserInfo) *sts.Credentials {
	if user.ConsoleReader == nil {
		user.ConsoleReader = console.DefaultConsoleReader{}
	}

	if user.StsClient == nil {
		awsSession, err := session.NewSession()
		if err != nil {
			log.Fatal(err)
		}

		user.StsClient = sts.New(awsSession)
	}

	var username string
	if len(user.User) == 0 {
		username, _ = user.ConsoleReader.ReadLine(usernamePrompt)
	} else {
		username = user.User
	}

	var pass string
	if user.NoMask {
		pass, _ = user.ConsoleReader.ReadLine(passwordPrompt)
	} else {
		var err error
		pass, err = user.ConsoleReader.ReadPassword(passwordPrompt)
		if err != nil {
			log.Fatal(err)
		}
		fmt.Fprintln(os.Stderr)
	}

	oktaAuthResponse, err := oktaClient.Authenticate(username, pass)
	if err != nil {
		log.Fatal(err)
	}

	err = doMfa(oktaAuthResponse, oktaClient.GetHttpClient(), user.ConsoleReader)

	oktaSessionResponse, err := oktaClient.StartSession(oktaAuthResponse.SessionToken)
	oktaClient.SetSessionId(oktaSessionResponse.Id)

	oktaApplications, err := oktaClient.ListApplications(oktaSessionResponse.UserId)

	app, found := findApp(user.Account, oktaApplications)
	if !found {
		// select an account
		fmt.Fprintln(os.Stderr, "Available accounts:")
		for idx, a := range oktaApplications {
			if a.AppName == "amazon_aws" {
				os.Stderr.WriteString(fmt.Sprintf("[%d] %s\n", idx, a.Label))
			}
		}
		var accountId int
		if accountId, err = user.ConsoleReader.ReadInt("Select an account: "); err != nil {
			log.Fatal(err)
		}
		app = &oktaApplications[accountId]
	}

	samlResponse, saml, err := oktaClient.GetSaml(*app)

	var arns []string
	for _, v := range samlResponse.Assertion.AttributeStatement.Attributes {
		if v.Name == "https://aws.amazon.com/SAML/Attributes/Role" {
			arns = strings.Split(v.Value, ",")
		}
	}

	samlInput := &sts.AssumeRoleWithSAMLInput{
		PrincipalArn:  aws.String(arns[0]),
		RoleArn:       aws.String(arns[1]),
		SAMLAssertion: aws.String(saml),
	}

	out, err := user.StsClient.AssumeRoleWithSAML(samlInput)
	if err != nil {
		log.Fatal(err)
	}

	return out.Credentials
}

func doMfa(oktaAuthResponse *okta.OktaAuthResponse, client *http.Client, consoleReader console.ConsoleReader) error {
	if oktaAuthResponse.Status == "MFA_REQUIRED" {
		fmt.Fprintln(os.Stderr, "MFA Required")
		for idx, factor := range oktaAuthResponse.Embedded.Factors {
			fmt.Fprintf(os.Stderr, "%d - %s\n", idx, factor.FactorType)
		}

		var mfaIdx int
		var err error
		if mfaIdx, err = consoleReader.ReadInt("Select an available MFA option: "); err != nil {
			log.Fatal(err)
		}
		vurl := oktaAuthResponse.Embedded.Factors[mfaIdx].Links.Verify.Url

		body := fmt.Sprintf(`{"stateToken":"%s"}`, oktaAuthResponse.StateToken)
		authResponse, err := client.Post(vurl, "application/json", strings.NewReader(body))
		if err != nil {
			log.Fatal(err)
		}

		z, _ := ioutil.ReadAll(authResponse.Body)
		err = json.Unmarshal(z, &oktaAuthResponse)
		if err != nil {
			log.Fatal(err)
		}

		var code string
		if code, err = consoleReader.ReadLine("Code: "); err != nil {
			log.Fatal(err)
		}
		body = fmt.Sprintf(`{"stateToken":"%s","passCode":"%s"}`, oktaAuthResponse.StateToken, code)
		authResponse, err = client.Post(vurl, "application/json", strings.NewReader(body))
		if err != nil {
			log.Fatal(err)
		}

		z, _ = ioutil.ReadAll(authResponse.Body)
		err = json.Unmarshal(z, &oktaAuthResponse)
		if err != nil {
			log.Fatal(err)
		}
	}
	return nil
}

func findApp(app string, apps []okta.OktaAppLink) (foundApp *okta.OktaAppLink, found bool) {
	for _, v := range apps {
		if strings.ToLower(v.Label) == strings.ToLower(app) {
			return &v, true
		}
	}

	return nil, false
}
