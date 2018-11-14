package bmx

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"net/url"
	"os"
	"strings"

	"github.com/aws/aws-sdk-go/aws/session"

	"github.com/aws/aws-sdk-go/service/sts/stsiface"

	"github.com/Brightspace/bmx/okta"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/service/sts"
)

type oktaClient interface {
	Authenticate(username string, password string) (*okta.OktaAuthResponse, error)
	GetHttpClient() *http.Client
	GetBaseUrl() *url.URL
	StartSession(sessionToken string) (*okta.OktaSessionResponse, error)
	ListApplications(userId string) ([]okta.OktaAppLink, error)
	SetSessionId(id string)
	GetSaml(appLink okta.OktaAppLink) (okta.Saml2pResponse, string, error)
}

type PrintCmdOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string

	ConsoleReader ConsoleReader
	StsClient     stsiface.STSAPI
}

const (
	usernamePrompt = "Okta Username: "
	passwordPrompt = "Okta Password: "
)

func Print(oktaClient oktaClient, printOptions PrintCmdOptions) {
	consoleReader := printOptions.ConsoleReader
	if printOptions.ConsoleReader == nil {
		consoleReader = defaultConsoleReader{}
	}

	if printOptions.StsClient == nil {
		awsSession, err := session.NewSession()
		if err != nil {
			log.Fatal(err)
		}

		printOptions.StsClient = sts.New(awsSession)
	}

	var username string
	if len(printOptions.User) == 0 {
		username, _ = consoleReader.ReadLine(usernamePrompt)
	} else {
		username = printOptions.User
	}

	var pass string
	if printOptions.NoMask {
		pass, _ = consoleReader.ReadLine(passwordPrompt)
	} else {
		var err error
		pass, err = consoleReader.ReadPassword(passwordPrompt)
		if err != nil {
			log.Fatal(err)
		}
		fmt.Fprintln(os.Stderr)
	}

	oktaAuthResponse, err := oktaClient.Authenticate(username, pass)
	if err != nil {
		log.Fatal(err)
	}

	err = doMfa(oktaAuthResponse, oktaClient.GetHttpClient(), consoleReader)

	oktaSessionResponse, err := oktaClient.StartSession(oktaAuthResponse.SessionToken)
	oktaClient.SetSessionId(oktaSessionResponse.Id)

	oktaApplications, err := oktaClient.ListApplications(oktaSessionResponse.UserId)

	app, found := findApp(printOptions.Account, oktaApplications)
	if !found {
		// select an account
		fmt.Fprintln(os.Stderr, "Available accounts:")
		for idx, a := range oktaApplications {
			if a.AppName == "amazon_aws" {
				os.Stderr.WriteString(fmt.Sprintf("[%d] %s\n", idx, a.Label))
			}
		}
		var accountId int
		if accountId, err = consoleReader.ReadInt("Select an account: "); err != nil {
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

	out, err := printOptions.StsClient.AssumeRoleWithSAML(samlInput)
	if err != nil {
		log.Fatal(err)
	}

	printDefaultFormat(out.Credentials)
}

func doMfa(oktaAuthResponse *okta.OktaAuthResponse, client *http.Client, consoleReader ConsoleReader) error {
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

func dumpResponse(response *http.Response) {
	for m := range response.Header {
		fmt.Fprintf(os.Stderr, "%s=%s\n", m, response.Header[m])
	}

	o, _ := ioutil.ReadAll(response.Body)
	fmt.Fprintln(os.Stderr, string(o))
}

func findApp(app string, apps []okta.OktaAppLink) (foundApp *okta.OktaAppLink, found bool) {
	for _, v := range apps {
		if strings.ToLower(v.Label) == strings.ToLower(app) {
			return &v, true
		}
	}

	return nil, false
}

func printPowershell(credentials *sts.Credentials) {
	fmt.Printf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) {
	fmt.Printf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
