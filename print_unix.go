// +build linux darwin freebsd netbsd openbsd

package bmx

import (
	"github.com/aws/aws-sdk-go/service/sts"
)

func printDefaultFormat(credentials *sts.Credentials) {
	printBash(credentials)
}
