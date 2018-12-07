// +build linux darwin freebsd netbsd openbsd

package cmd

import (
	"github.com/aws/aws-sdk-go/service/sts"
)

func printDefaultFormat(credentials *sts.Credentials) {
	printBash(credentials)
}
