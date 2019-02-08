package bmx

import (
	"fmt"
	"log"

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
	Role     string
}

func GetUserInfoFromPrintCmdOptions(printOptions PrintCmdOptions) serviceProviders.UserInfo {
	user := serviceProviders.UserInfo{
		Org:      printOptions.Org,
		User:     printOptions.User,
		Account:  printOptions.Account,
		NoMask:   printOptions.NoMask,
		Password: printOptions.Password,
		Role:     printOptions.Role,
	}
	return user
}

func Print(idProvider identityProviders.IdentityProvider, printOptions PrintCmdOptions) {
	printOptions.User = getUserIfEmpty(printOptions.User)
	user := GetUserInfoFromPrintCmdOptions(printOptions)

	saml, err := authenticate(user, idProvider)
	if err != nil {
		log.Fatal(err)
	}

	creds := AwsServiceProvider.GetCredentials(saml, printOptions.Role)
	printDefaultFormat(creds)
}

func printPowershell(credentials *sts.Credentials) {
	fmt.Printf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) {
	fmt.Printf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
