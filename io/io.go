package io

type Reader interface {
	ReadInt(prompt string) (int, error)
	ReadLine(prompt string) (string, error)
	ReadPassword(prompt string) (string, error)
}

type Writer interface {
	Writeln(message string)
}

type ReadWriter interface {
	Reader
	Writer
}
