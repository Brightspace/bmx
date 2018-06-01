package bmx

import (
	"bufio"
	"encoding/base64"
	"encoding/json"
	"encoding/xml"
	"flag"
	"fmt"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"net/http/cookiejar"
	"os"
	"strconv"
	"strings"
	"time"

	"github.com/Brightspace/bmx/okta"
	"github.com/Brightspace/bmx/password"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/sts"
	"golang.org/x/net/html"
	"golang.org/x/net/publicsuffix"
)

type printOptions struct {
	Url     string
	Account string
	User    string
	NoMask  bool
	Org     string
}

var options = printOptions{}

var printCmdLine = flag.NewFlagSet("print", flag.ExitOnError)

func init() {
	printCmdLine.StringVar(&options.Org, "org", "", "the okta org api to target")
	printCmdLine.StringVar(&options.Account, "account", "", "the account name to auth against")
	printCmdLine.StringVar(&options.User, "user", "", "the user to authenticate with")
	printCmdLine.BoolVar(&options.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")
}

func readLine(prompt string) (string, error) {
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Fprint(os.Stderr, prompt)
	var s string
	scanner.Scan()
	if scanner.Err() != nil {
		return "", scanner.Err()
	}
	s = scanner.Text()
	return s, nil
}

func readInt(prompt string) (int, error) {
	var s string
	var err error
	if s, err = readLine(prompt); err != nil {
		return -1, err
	}

	var i int
	if i, err = strconv.Atoi(s); err != nil {
		return -1, err
	}

	return i, nil
}

type Options struct {
	Command string
}

const (
	usernamePrompt = "Okta Username: "
	passwordPrompt = "Okta Password: "
)

func Print(config Options, args []string) {
	err := printCmdLine.Parse(args)
	if err != nil {
		log.Fatal(err)
	}

	var username string
	if len(options.User) == 0 {
		username, _ = readLine(usernamePrompt)
	} else {
		username = options.User
	}

	var pass string
	if options.NoMask {
		pass, _ = readLine(passwordPrompt)
	} else {
		var err error
		pass, err = password.Read(passwordPrompt)
		if err != nil {
			log.Fatal(err)
		}
	}

	// All users of cookiejar should import "golang.org/x/net/publicsuffix"
	jar, err := cookiejar.New(&cookiejar.Options{PublicSuffixList: publicsuffix.List})
	if err != nil {
		log.Fatal(err)
	}
	httpClient := &http.Client{
		Timeout: 30 * time.Second,
		Jar:     jar,
	}

	oktaClient, _ := okta.NewOktaClient(httpClient, options.Org)

	oktaAuthResponse, err := oktaClient.Authenticate(username, pass)
	if err != nil {
		log.Fatal(err)
	}

	err = doMfa(oktaAuthResponse, httpClient)

	oktaSessionResponse, err := oktaClient.StartSession(oktaAuthResponse.SessionToken)
	cookies := jar.Cookies(oktaClient.BaseUrl)
	cookie := &http.Cookie{
		Name:     "sid",
		Value:    oktaSessionResponse.Id,
		Path:     "/",
		Domain:   oktaClient.BaseUrl.Host,
		Secure:   true,
		HttpOnly: true,
	}
	cookies = append(cookies, cookie)
	jar.SetCookies(oktaClient.BaseUrl, cookies)

	oktaApplications, err := oktaClient.ListApplications(oktaSessionResponse.UserId)

	app, found := findApp(options.Account, oktaApplications)
	if !found {
		// select an account
		fmt.Fprintln(os.Stderr, "Available accounts:")
		for idx, a := range oktaApplications {
			if a.AppName == "amazon_aws" {
				os.Stderr.WriteString(fmt.Sprintf("[%d] %s\n", idx, a.Label))
			}
		}
		var accountId int
		if accountId, err = readInt("Select an account: "); err != nil {
			log.Fatal(err)
		}
		app = &oktaApplications[accountId]
	}

	appResponse, err := httpClient.Get(app.LinkUrl)
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

func doMfa(oktaAuthResponse *okta.OktaAuthResponse, client *http.Client) error {
	if oktaAuthResponse.Status == "MFA_REQUIRED" {
		fmt.Fprintln(os.Stderr, "MFA Required")
		for idx, factor := range oktaAuthResponse.Embedded.Factors {
			fmt.Fprintf(os.Stderr, "%d - %s\n", idx, factor.FactorType)
		}

		var mfaIdx int
		var err error
		if mfaIdx, err = readInt("Select an available MFA option: "); err != nil {
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
		if code, err = readLine("Code: "); err != nil {
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
	fmt.Printf(`$env:AWS_SESSION_TOKEN="%s"; $env:AWS_ACCESS_KEY_ID="%s"; $env:AWS_SECRET_ACCESS_KEY="%s"`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
