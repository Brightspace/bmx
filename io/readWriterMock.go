package io

type mockReadWriter struct {
	readInt      func(prompt string) (int, error)
	readLine     func(prompt string) (string, error)
	readPassword func(prompt string) (string, error)
	writeln      func(string)
}

func NewMockReadWriter() *mockReadWriter {
	m := &mockReadWriter{}
	m.MockWriteln(func(string) {})

	return m
}

func (m *mockReadWriter) ReadInt(prompt string) (int, error) {
	return m.readInt(prompt)
}

func (m *mockReadWriter) ReadLine(prompt string) (string, error) {
	return m.readLine(prompt)
}

func (m *mockReadWriter) ReadPassword(prompt string) (string, error) {
	return m.readPassword(prompt)
}

func (m *mockReadWriter) Writeln(message string) {
	m.writeln(message)
}

func (m *mockReadWriter) MockReadInt(fn func(string) (int, error)) {
	m.readInt = fn
}

func (m *mockReadWriter) MockReadLine(fn func(string) (string, error)) {
	m.readLine = fn
}

func (m *mockReadWriter) MockReadPassword(fn func(string) (string, error)) {
	m.readPassword = fn
}

func (m *mockReadWriter) MockWriteln(fn func(string)) {
	m.writeln = fn
}
