package serviceProviders

import (
	"github.com/aws/aws-sdk-go/service/sts"
)

type ServiceProvider interface {
	GetCredentials(role Role, saml string) (*sts.Credentials, error)
	ListRoles(encodedSaml string) ([]Role, error)
}
