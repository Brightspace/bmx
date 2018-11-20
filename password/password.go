// this package is heavily based on https://github.com/hashicorp/vault/tree/master/helper/password

// password is a package for reading a password securely from a terminal.
// The code in this package disables echo in the terminal so that the
// password is not echoed back in plaintext to the user.
package password

import (
	"errors"
	"fmt"
	"os"

	pwd "github.com/hashicorp/vault/helper/password"
)

var ErrInterrupted = errors.New("interrupted")

func Read(prompt string) (string, error) {
	fmt.Fprint(os.Stderr, prompt)
	result, err := pwd.Read(os.Stdin)

	// This discards up to 100 excess bytes on stdin in case we get extra garbage returned after eol has been detected as is the case on Windows with the \r\n pattern
	var buf [100]byte
	os.Stdin.Read(buf[:])

	return result, err
}
