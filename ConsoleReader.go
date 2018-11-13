package bmx

import (
	"bufio"
	"fmt"
	"os"
	"strconv"

	"github.com/Brightspace/bmx/password"
)

type ConsoleReader interface {
	ReadLine(prompt string) (string, error)
	ReadPassword(prompt string) (string, error)
	ReadInt(prompt string) (int, error)
}

type defaultConsoleReader struct{}

func (r defaultConsoleReader) ReadLine(prompt string) (string, error) {
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

func (r defaultConsoleReader) ReadInt(prompt string) (int, error) {
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

func (r defaultConsoleReader) ReadPassword(prompt string) (string, error) {
	return password.Read(prompt)
}
