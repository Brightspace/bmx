package bmx

import (
	"fmt"
	"io/ioutil"
	"net/http"
	"net/url"
	"os"

	"github.com/Brightspace/bmx/okta"
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
}

func GetUserInfoFromPrintCmdOptions(printOptions PrintCmdOptions) UserInfo {
	user := UserInfo{
		Org:      printOptions.Org,
		User:     printOptions.User,
		Account:  printOptions.Account,
		NoMask:   printOptions.NoMask,
		Password: printOptions.Password,
	}

	return user
}

func Print(oktaClient oktaClient, printOptions PrintCmdOptions) {
	creds := GetCredentials(oktaClient, GetUserInfoFromPrintCmdOptions(printOptions))
	printDefaultFormat(creds)
}

func dumpResponse(response *http.Response) {
	for m := range response.Header {
		fmt.Fprintf(os.Stderr, "%s=%s\n", m, response.Header[m])
	}

	o, _ := ioutil.ReadAll(response.Body)
	fmt.Fprintln(os.Stderr, string(o))
}

func printPowershell(credentials *sts.Credentials) {
	fmt.Printf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) {
	fmt.Printf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
