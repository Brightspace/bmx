package okta_test

import (
	"log"
	"net/http"
	"net/http/httptest"
	"net/url"
	"testing"
	"time"

	"github.com/Brightspace/bmx/saml/identityProviders/okta"
	"github.com/Brightspace/bmx/saml/identityProviders/okta/file"
	"github.com/Brightspace/bmx/saml/identityProviders/okta/mocks"
)

var (
	server *httptest.Server
)

type OktaClient struct {
	*okta.OktaClient
	SessionCache mocks.SessionCache
}

func NewOktaClient() *OktaClient {
	client, _ := okta.NewOktaClient("test")
	o := &OktaClient{
		OktaClient:   client,
		SessionCache: mocks.DefaultSessionCache(),
	}
	o.OktaClient.SessionCache = &o.SessionCache

	return o
}

func TestAuthenticateFromCacheWithNoExistingSession(t *testing.T) {
	client := NewOktaClient()
	client.SessionCache.SessionsFn = func() ([]file.OktaSessionCache, error) {
		return nil, nil
	}
	userID, ok := client.AuthenticateFromCache("user", "org")
	if ok {
		t.Errorf("Expected ok to be false, but it was true")
	}
	if userID != "" {
		t.Errorf("Expected userID to be empty, but was %v", userID)
	}
}

func TestAuthenticateFromCacheWithExistingSession(t *testing.T) {
	client := NewOktaClient()
	client.BaseUrl = openServer([]byte(`{"Id":"someid"}`))
	defer closeServer()

	sessions := []file.OktaSessionCache{
		file.OktaSessionCache{
			Userid:    "blah",
			Org:       "org",
			SessionId: "Id",
			ExpiresAt: time.Now().Local().Add(time.Hour * 2).Format(time.RFC3339),
		},
	}
	client.SessionCache.SessionsFn = func() ([]file.OktaSessionCache, error) {
		return sessions, nil
	}
	userID, ok := client.AuthenticateFromCache("blah", "org")
	if !ok {
		t.Errorf("Expected ok to be true, but it was false")
	}
	if userID != "someid" {
		t.Errorf("Expected userID to be someid, but was %v", userID)
	}
}

func TestAuthenticateFromCacheWithExpiredExistingSession(t *testing.T) {
	client := NewOktaClient()
	client.BaseUrl = openServer([]byte(`{"Id":"someid"}`))
	defer closeServer()

	sessions := []file.OktaSessionCache{
		file.OktaSessionCache{
			Userid:    "blah",
			Org:       "org",
			SessionId: "Id",
			ExpiresAt: time.Now().Local().Add(-(time.Hour * 2)).Format(time.RFC3339),
		},
	}
	client.SessionCache.SessionsFn = func() ([]file.OktaSessionCache, error) {
		return sessions, nil
	}
	userID, ok := client.AuthenticateFromCache("blah", "org")
	if ok {
		t.Errorf("Expected ok to be false, but it was true")
	}
	if userID != "" {
		t.Errorf("Expected userID to be empty, but was %v", userID)
	}
}

func openServer(response []byte) *url.URL {
	server = httptest.NewServer(http.HandlerFunc(func(rw http.ResponseWriter, req *http.Request) {
		// equals(t, req.URL.String(), "/some/path")
		rw.Write(response)
	}))

	u, err := url.Parse(server.URL)
	if err != nil {
		log.Fatal(err)
	}
	return u
}

func closeServer() {
	server.Close()
}
