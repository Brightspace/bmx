package mocks

import (
	"github.com/Brightspace/bmx/okta/file"
)

type SessionCache struct {
	SaveSessionsFn        func(session []file.OktaSessionCache)
	SaveSessionsFnInvoked bool
	SessionsFn            func() ([]file.OktaSessionCache, error)
	SessionsFnInvoked     bool
}

func (s *SessionCache) SaveSessions(sessions []file.OktaSessionCache) {
	s.SaveSessionsFnInvoked = true
	s.SaveSessionsFn(sessions)
}

func (s *SessionCache) Sessions() ([]file.OktaSessionCache, error) {
	s.SessionsFnInvoked = true
	return s.SessionsFn()
}

func DefaultSessionCache() SessionCache {
	return SessionCache{
		SaveSessionsFn: func(session []file.OktaSessionCache) {},
		SessionsFn:     func() ([]file.OktaSessionCache, error) { return nil, nil },
	}
}
