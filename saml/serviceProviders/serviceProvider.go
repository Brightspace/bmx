package serviceProviders

import (
	"github.com/Brightspace/bmx/saml/identityProviders"
	"github.com/aws/aws-sdk-go/service/sts"
)

type ServiceProvider interface {
	GetCredentials(idProvider identityProviders.IdentityProvider, user UserInfo) *sts.Credentials
}
