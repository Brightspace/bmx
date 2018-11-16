package okta_test

import (
	"testing"

	"github.com/Brightspace/bmx/okta"
	"github.com/Brightspace/bmx/okta/file"
	"github.com/Brightspace/bmx/okta/mocks"
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

func TestOkta_GetSamlWithNoExistingSession(t *testing.T) {
	client := NewOktaClient()
	client.SessionCache.SessionsFn = func() ([]file.OktaSessionCache, error) {
		return nil, nil
	}
}
