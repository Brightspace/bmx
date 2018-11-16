package bmx

import (
	"github.com/Brightspace/bmx/saml/identityProviders"
	"github.com/Brightspace/bmx/saml/serviceProviders"
	"github.com/aws/aws-sdk-go/service/sts"
)

type ServiceProvider interface {
	GetCredentials(idProvider identityProviders.IdentityProvider, user serviceProviders.UserInfo) *sts.Credentials
}
