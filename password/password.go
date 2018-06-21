// this package is heavily based on https://github.com/hashicorp/vault/tree/master/helper/password

// password is a package for reading a password securely from a terminal.
// The code in this package disables echo in the terminal so that the
// password is not echoed back in plaintext to the user.
package password

import (
	"errors"
	"fmt"
	"io"
	"os"
)

var ErrInterrupted = errors.New("interrupted")

func Read(prompt string) (string, error) {
	fmt.Fprint(os.Stderr, prompt)
	return read(os.Stdin)
}

func readLine(f *os.File) (string, error) {
	var buf [1]byte
	resultBuf := make([]byte, 0, 64)
	for {
		n, err := f.Read(buf[:])
		if err != nil && err != io.EOF {
			return "", err
		}
		if n == 0 || buf[0] == '\n' || buf[0] == '\r' {
			break
		}

		// ASCII code 3 is what is sent for a Ctrl-C while reading raw.
		// If we see that, then get the interrupt. We have to do this here
		// because terminals in raw mode won't catch it at the shell level.
		if buf[0] == 3 {
			return "", ErrInterrupted
		}

		resultBuf = append(resultBuf, buf[0])
	}

	return string(resultBuf), nil
}
