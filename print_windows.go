package bmx

import (
	"github.com/Brightspace/bmx/io"
	"github.com/aws/aws-sdk-go/service/sts"
)

func printDefaultFormat(rw io.ReadWriter, credentials *sts.Credentials) {
	printPowershell(rw, credentials)
}
