package mocks

import ()

type ConsoleReaderMock struct{}

func (r ConsoleReaderMock) ReadLine(prompt string) (string, error) {
	return prompt, nil
}

func (r ConsoleReaderMock) ReadInt(prompt string) (int, error) {
	return 0, nil
}

func (r ConsoleReaderMock) ReadPassword(prompt string) (string, error) {
	return prompt, nil
}
