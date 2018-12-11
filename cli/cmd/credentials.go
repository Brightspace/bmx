package cmd

import (
	"github.com/Brightspace/bmx/cli/console"
	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/serviceProviders"

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

	saml, err := authenticate(reader, username, noMask, filter, account, identityClient)
	if err != nil {
		return nil, err
	}

	creds, err := selectRole(reader, role, saml, serviceClient)
	if err != nil {
		return nil, err
	}

	return creds, nil
}
