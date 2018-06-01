package bmx

import (
	"github.com/aws/aws-sdk-go/service/sts"
)

func printDefaultFormat(credentials *sts.Credentials) {
	printPowershell(credentials)
}
