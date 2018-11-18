package serviceProviders

import (
	"github.com/aws/aws-sdk-go/service/sts"
)

type ServiceProvider interface {
	GetCredentials(saml string) *sts.Credentials
}
