/*
Copyright 2019 D2L Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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

const (
	Bash       = "bash"
	Powershell = "powershell"
)

type PrintCmdOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string
	Role     string
	Output   string
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

func Print(idProvider identityProviders.IdentityProvider, printOptions PrintCmdOptions) string {
	printOptions.User = getUserIfEmpty(printOptions.User)
	user := GetUserInfoFromPrintCmdOptions(printOptions)

	saml, err := authenticate(user, idProvider)
	if err != nil {
		log.Fatal(err)
	}

	creds := AwsServiceProvider.GetCredentials(saml, printOptions.Role)
	command := printCommand(printOptions, creds)
	return command
}

func printCommand(printOptions PrintCmdOptions, creds *sts.Credentials) string {
	switch printOptions.Output {
	case Powershell:
		return printPowershell(creds)
	case Bash:
		return printBash(creds)
	}
	return printDefaultFormat(creds)
}

func printPowershell(credentials *sts.Credentials) string {
	return fmt.Sprintf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) string {
	return fmt.Sprintf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
