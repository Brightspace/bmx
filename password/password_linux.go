// +build linux darwin freebsd netbsd openbsd

package password

import (
	"fmt"
	"os"

	"golang.org/x/crypto/ssh/terminal"
)

func read() (string, error) {
	fd := int(os.Stdin.Fd())
	if !terminal.IsTerminal(fd) {
		return "", fmt.Errorf("file descriptor %d is not a terminal", fd)
	}

	oldState, err := terminal.MakeRaw(fd)
	if err != nil {
		return "", err
	}
	defer terminal.Restore(fd, oldState)

	return readLine()
}
