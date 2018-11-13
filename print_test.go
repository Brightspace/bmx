package bmx_test

import (
	"fmt"
	"strconv"
	"testing"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/okta"
)

func TestHelloWorld(t *testing.T) {
	fmt.Println("Hello world!")
}

type mokta struct {
	okta.OktaClient
}

func (m *mokta) Authenticate(username string, password string) (*okta.OktaAuthResponse, error) {
	response := &okta.OktaAuthResponse{}
	return response, nil
}

func (m *mokta) StartSession(sessionToken string) (*okta.OktaSessionResponse, error) {
	response := &okta.OktaSessionResponse{}
	return response, nil
}

func (m *mokta) SetSessionId(id string) {

}

func (m *mokta) ListApplications(userId string) ([]okta.OktaAppLink, error) {
	response := []okta.OktaAppLink{
		{Id: "id", Label: "label", LinkUrl: "url", AppName: "appname", AppInstanceId: "instanceid"},
	}

	return response, nil
}

type queuedConsoleReader struct {
	ResponseItems []string
	currentItem   int
}

func (q *queuedConsoleReader) ReadLine(prompt string) (string, error) {
	r := q.ResponseItems[q.currentItem]
	q.currentItem++
	return r, nil
}
func (q *queuedConsoleReader) ReadPassword(prompt string) (string, error) {
	r := q.ResponseItems[q.currentItem]
	q.currentItem++
	return r, nil
}
func (q *queuedConsoleReader) ReadInt(prompt string) (int, error) {
	r, _ := strconv.Atoi(q.ResponseItems[q.currentItem])
	q.currentItem++
	return r, nil
}

func TestMonkey(t *testing.T) {
	cr := &queuedConsoleReader{
		ResponseItems: []string{
			"myuser",
			"mypassword",
			"1",
		},
	}
	options := bmx.PrintCmdOptions{
		Org:           "myorg",
		ConsoleReader: cr,
	}

	oktaClient := &mokta{}

	bmx.Print(oktaClient, options)
}
