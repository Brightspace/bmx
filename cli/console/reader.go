package console

import (
	"bufio"
	"fmt"
	"os"
	"strconv"
	"syscall"

	"golang.org/x/crypto/ssh/terminal"
)

type Reader struct{}

func (r Reader) ReadLine(prompt string) (string, error) {
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Fprint(os.Stderr, prompt)
	var s string
	scanner.Scan()
	if scanner.Err() != nil {
		return "", scanner.Err()
	}
	s = scanner.Text()
	return s, nil
}

func (r Reader) ReadInt(prompt string) (int, error) {
	var s string
	var err error
	if s, err = r.ReadLine(prompt); err != nil {
		return -1, err
	}

	var i int
	if i, err = strconv.Atoi(s); err != nil {
		return -1, err
	}

	return i, nil
}

func (r Reader) ReadPassword(prompt string) (string, error) {
	fmt.Fprint(os.Stderr, prompt)
	var pass, err = terminal.ReadPassword(syscall.Stdin)

	if err != nil {
		return "", err
	}

	return string(pass[:]), nil
}
