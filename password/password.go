// this package is heavily based on https://github.com/hashicorp/vault/tree/master/helper/password

// password is a package for reading a password securely from a terminal.
// The code in this package disables echo in the terminal so that the
// password is not echoed back in plaintext to the user.
package password

import (
	"bufio"
	"errors"
	"fmt"
	"os"
)

var ErrInterrupted = errors.New("interrupted")

func Read(prompt string) (string, error) {
	fmt.Fprint(os.Stderr, prompt)
	return read()
}

func readLine() (string, error) {
	scanner := bufio.NewScanner(os.Stdin)
	var s string
	scanner.Scan()
	if scanner.Err() != nil {
		return "", scanner.Err()
	}
	s = scanner.Text()
	return s, nil
}