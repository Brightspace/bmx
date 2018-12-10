package console

type mockReader struct {
	readInt      func(prompt string) (int, error)
	readLine     func(prompt string) (string, error)
	readPassword func(prompt string) (string, error)
}

func NewMockReader() *mockReader {
	return &mockReader{}
}

func (m *mockReader) ReadInt(prompt string) (int, error) {
	return m.readInt(prompt)
}

func (m *mockReader) ReadLine(prompt string) (string, error) {
	return m.readLine(prompt)
}

func (m *mockReader) ReadPassword(prompt string) (string, error) {
	return m.readPassword(prompt)
}

func (m *mockReader) MockReadInt(fn func(string) (int, error)) {
	m.readInt = fn
}

func (m *mockReader) MockReadLine(fn func(string) (string, error)) {
	m.readLine = fn
}

func (m *mockReader) MockReadPassword(fn func(string) (string, error)) {
	m.readPassword = fn
}
