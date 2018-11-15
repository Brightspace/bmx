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
