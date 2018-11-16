package serviceProviders

import (
	"github.com/Brightspace/bmx/console"
)

type UserInfo struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string

	ConsoleReader console.ConsoleReader
}
