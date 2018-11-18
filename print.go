package bmx

import (
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
	"os"

	"github.com/Brightspace/bmx/saml/identityProviders"

	"github.com/Brightspace/bmx/saml/serviceProviders"
	"github.com/Brightspace/bmx/saml/serviceProviders/aws"
	"github.com/aws/aws-sdk-go/service/sts"
)

var (
	AwsServiceProvider serviceProviders.ServiceProvider
)

func init() {
	AwsServiceProvider = aws.NewAwsServiceProvider()
}

type PrintCmdOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string
}

func GetUserInfoFromPrintCmdOptions(printOptions PrintCmdOptions) serviceProviders.UserInfo {
	user := serviceProviders.UserInfo{
		Org:      printOptions.Org,
		User:     printOptions.User,
		Account:  printOptions.Account,
		NoMask:   printOptions.NoMask,
		Password: printOptions.Password,
	}
	return user
}

func Print(idProvider identityProviders.IdentityProvider, printOptions PrintCmdOptions) {
	printOptions.User, printOptions.Password = getCredentials(printOptions.User, printOptions.NoMask)
	user := GetUserInfoFromPrintCmdOptions(printOptions)

	saml, err := authenticate(user, idProvider)
	if err != nil {
		log.Fatal(err)
	}

	creds := AwsServiceProvider.GetCredentials(saml)
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
