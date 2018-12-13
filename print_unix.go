// +build linux darwin freebsd netbsd openbsd

package bmx

import (
	"github.com/Brightspace/bmx/io"
	"github.com/aws/aws-sdk-go/service/sts"
)

func printDefaultFormat(rw io.ReadWriter, credentials *sts.Credentials) {
	printBash(rw, credentials)
}
