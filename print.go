package bmx

import (
	"encoding/base64"
	"encoding/json"
	"encoding/xml"
	"fmt"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"net/url"
	"os"
	"strings"

	"github.com/Brightspace/bmx/okta"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/sts"
	"golang.org/x/net/html"
)

type oktaClient interface {
	Authenticate(username string, password string) (*okta.OktaAuthResponse, error)
	GetHttpClient() *http.Client
	GetBaseUrl() *url.URL
	StartSession(sessionToken string) (*okta.OktaSessionResponse, error)
	ListApplications(userId string) ([]okta.OktaAppLink, error)
	SetSessionId(id string)
}

type PrintCmdOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string

	ConsoleReader ConsoleReader
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

	appResponse, err := oktaClient.GetHttpClient().Get(app.LinkUrl)
	if err != nil {
		log.Fatal(err)
	}

	saml, err := getSaml(appResponse.Body)
	decSaml, err := base64.StdEncoding.DecodeString(saml)

	samlResponse := &Saml2pResponse{}
	err = xml.Unmarshal(decSaml, samlResponse)
	if err != nil {
		log.Fatal(err)
	}

	var arns []string
	for _, v := range samlResponse.Assertion.AttributeStatement.Attributes {
		if v.Name == "https://aws.amazon.com/SAML/Attributes/Role" {
			arns = strings.Split(v.Value, ",")
		}
	}

	awsSession, err := session.NewSession()
	if err != nil {
		log.Fatal(err)
	}
	svc := sts.New(awsSession)
	samlInput := &sts.AssumeRoleWithSAMLInput{
		PrincipalArn:  aws.String(arns[0]),
		RoleArn:       aws.String(arns[1]),
		SAMLAssertion: aws.String(saml),
	}

	out, err := svc.AssumeRoleWithSAML(samlInput)
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

func getSaml(r io.Reader) (string, error) {
	z := html.NewTokenizer(r)
	for {
		tt := z.Next()
		switch tt {
		case html.ErrorToken:
			return "", z.Err()
		case html.SelfClosingTagToken:
			tn, hasAttr := z.TagName()

			if string(tn) == "input" {
				attr := make(map[string]string)
				for hasAttr {
					key, val, moreAttr := z.TagAttr()
					attr[string(key)] = string(val)
					if !moreAttr {
						break
					}
				}

				if attr["name"] == "SAMLResponse" {
					return string(attr["value"]), nil
				}
			}
		}
	}

	return "", nil
}

type Saml2pResponse struct {
	XMLName   xml.Name       `xml:"Response"`
	Assertion Saml2Assertion `xml:"Assertion"`
}

type Saml2Assertion struct {
	XMLName            xml.Name                `xml:"Assertion"`
	AttributeStatement Saml2AttributeStatement `xml:"AttributeStatement"`
}

type Saml2AttributeStatement struct {
	XMLName    xml.Name         `xml:"AttributeStatement"`
	Attributes []Saml2Attribute `xml:"Attribute"`
}

type Saml2Attribute struct {
	Name  string `xml:"Name,attr"`
	Value string `xml:"AttributeValue"`
}

func printPowershell(credentials *sts.Credentials) {
	fmt.Printf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) {
	fmt.Printf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
