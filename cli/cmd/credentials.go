package cmd

import (
	"github.com/toddradigan/bmx-go/bmx/identityProviders"

	"github.com/toddradigan/bmx-go/bmx/serviceProviders"
	"github.com/toddradigan/bmx-go/cli/console"

	"github.com/aws/aws-sdk-go/service/sts"
)

func getCredentials(
	reader console.Reader,
	identityClient identityProviders.IdentityProvider,
	serviceClient serviceProviders.ServiceProvider,
	username string,
	noMask bool,
	filter string,
	account string,
	role string,
) (*sts.Credentials, error) {

	saml, err := authenticate(consoleReader, username, noMask, filter, account, identityClient)
	if err != nil {
		return nil, err
	}

	creds, err := selectRole(consoleReader, role, saml, serviceClient)
	if err != nil {
		return nil, err
	}

	return creds, nil
}
