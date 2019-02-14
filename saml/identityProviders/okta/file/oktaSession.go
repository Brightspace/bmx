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

package file

import (
	"encoding/json"
	"io/ioutil"
	"os"
	"path"
	"runtime"
)

type OktaSessionCache struct {
	Userid    string `json:"userId"`
	Org       string `json:"org"`
	SessionId string `json:"sessionId"`
	ExpiresAt string `json:"expiresAt"`
}

type OktaSessionStorage struct{}

func (o *OktaSessionStorage) SaveSessions(sessions []OktaSessionCache) {
	sessionsJSON, _ := json.Marshal(sessions)
	ioutil.WriteFile(path.Join(userHomeDir(), ".bmx", "sessions"), sessionsJSON, 0644)
}

func (o *OktaSessionStorage) Sessions() ([]OktaSessionCache, error) {
	sessionsFile, err := ioutil.ReadFile(path.Join(userHomeDir(), ".bmx", "sessions"))
	if os.IsNotExist(err) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	var sessions []OktaSessionCache
	json.Unmarshal([]byte(sessionsFile), &sessions)
	return sessions, nil
}

func userHomeDir() string {
	if runtime.GOOS == "windows" {
		home := os.Getenv("HOMEDRIVE") + os.Getenv("HOMEPATH")
		if home == "" {
			home = os.Getenv("USERPROFILE")
		}
		return home
	}
	return os.Getenv("HOME")
}
