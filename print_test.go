package bmx_test

import (
	"testing"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/mocks"
)

func TestMonkey(t *testing.T) {
	options := bmx.PrintCmdOptions{
		Org: "myorg",
	}

	oktaClient := &mocks.Mokta{}

	bmx.AwsServiceProvider = &mocks.AwsServiceProviderMock{}
	bmx.ConsoleReader = mocks.ConsoleReaderMock{}
	bmx.Print(oktaClient, options)
}
