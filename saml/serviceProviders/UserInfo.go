package serviceProviders

import (
	"github.com/Brightspace/bmx/console"
	"github.com/aws/aws-sdk-go/service/sts/stsiface"
)

type UserInfo struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string

	ConsoleReader console.ConsoleReader
	StsClient     stsiface.STSAPI
}
