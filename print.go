package bmx

import (
	"fmt"

	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/io"
	"github.com/Brightspace/bmx/serviceProviders"
	"github.com/aws/aws-sdk-go/service/sts"
)

func Print(
	rw io.ReadWriter,
	identity identityProviders.IdentityProvider,
	service serviceProviders.ServiceProvider,
	username string,
	noMask bool,
	filter string,
	account string,
	role string,
) error {

	creds, err := getCredentials(
		rw,
		identity,
		service,
		username,
		noMask,
		filter,
		account,
		role,
	)
	if err != nil {
		return err
	}

	printDefaultFormat(rw, creds)

	return nil
}

func printPowershell(rw io.ReadWriter, credentials *sts.Credentials) {
	rw.Writeln(fmt.Sprintf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey))
}

func printBash(rw io.ReadWriter, credentials *sts.Credentials) {
	rw.Writeln(fmt.Sprintf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey))
}
