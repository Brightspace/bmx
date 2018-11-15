package okta_test

import (
	"testing"

	"github.com/Brightspace/bmx/okta"
	"github.com/Brightspace/bmx/okta/mocks"
)

func TestOkta_GetSamlWithNoExistingSession(t *testing.T) {
	client, err := okta.NewOktaClient("test")
	client.SessionCache = mocks.DefaultSessionCache()

	c := MustOpenClient()
	defer c.Close()
	s := c.Connect().DialService()
	// Mock authentication.
	c.Authenticator.AuthenticateFn = func(_ string) (*wtf.User, error) {
		return &wtf.User{ID: 123}, nil
	}
	dial := wtf.Dial{
		Name:  "MY DIAL",
		Level: 50,
	}
	// Create new dial.
	if err := s.CreateDial(&dial); err != nil {
		t.Fatal(err)
	} else if dial.ID != 1 {
		t.Fatalf("unexpected id: %d", dial.ID)
	} else if dial.UserID != 123 {
		t.Fatalf("unexpected user id: %d", dial.UserID)
	}
	// Retrieve dial and compare.
	other, err := s.Dial(1)
	if err != nil {
		t.Fatal(err)
	} else if !reflect.DeepEqual(&dial, other) {
		t.Fatalf("unexpected dial: %#v", other)
	}
}
