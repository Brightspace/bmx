package bmx

import (
	"github.com/Brightspace/bmx/serviceProviders"
	"github.com/aws/aws-sdk-go/service/sts"
)

type ServiceProvider interface {
	GetCredentials(oktaClient serviceProviders.IOktaClient, user serviceProviders.UserInfo) *sts.Credentials
}
