/*
Copyright 2019 D2L Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package mocks

import (
	"github.com/Brightspace/bmx/saml/identityProviders/okta/file"
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
