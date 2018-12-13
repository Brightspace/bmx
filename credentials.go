package bmx

import (
	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/io"
	"github.com/Brightspace/bmx/serviceProviders"

	"github.com/aws/aws-sdk-go/service/sts"
)

func getCredentials(
	rw io.ReadWriter,
	identityClient identityProviders.IdentityProvider,
	serviceClient serviceProviders.ServiceProvider,
	username string,
	noMask bool,
	filter string,
	account string,
	role string,
) (*sts.Credentials, error) {

	saml, err := authenticate(rw, username, noMask, filter, account, identityClient)
	if err != nil {
		return nil, err
	}

	creds, err := selectRole(rw, role, saml, serviceClient)
	if err != nil {
		return nil, err
	}

	return creds, nil
}
