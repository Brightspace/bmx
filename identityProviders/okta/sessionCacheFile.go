package okta

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"os"
	"path"
	"runtime"
	"time"
)

type oktaSessionFileCache struct {
	org      string
	sessions []oktaSessionCache
}

func NewOktaSessionFileCache(org string) *oktaSessionFileCache {
	return &oktaSessionFileCache{
		org: org,
	}
}

func (f *oktaSessionFileCache) Save(sessions []oktaSessionCache) error {
	currTime := time.Now()

	activeSessions := make([]oktaSessionCache, 0)
	for _, session := range sessions {
		expireTime, err := time.Parse(time.RFC3339, session.ExpiresAt)
		if err == nil && expireTime.After(currTime) {
			activeSessions = append(activeSessions, session)
		}
	}

	f.sessions = activeSessions

	sessionsJSON, err := json.Marshal(activeSessions)
	if err != nil {
		return err
	}

	cacheFilePath := f.sessionsFile()
	return ioutil.WriteFile(cacheFilePath, sessionsJSON, 0644)
}

func (f *oktaSessionFileCache) Sessions() ([]oktaSessionCache, error) {
	if f.sessions != nil && len(f.sessions) > 0 {
		return f.sessions, nil
	}

	cacheFilePath := f.sessionsFile()
	sessionsFile, err := ioutil.ReadFile(cacheFilePath)
	if os.IsNotExist(err) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}

	var sessions []oktaSessionCache
	err = json.Unmarshal([]byte(sessionsFile), &sessions)
	if err != nil {
		return nil, err
	}

	f.sessions = sessions
	return sessions, nil
}

func (f *oktaSessionFileCache) sessionsFile() string {
	return path.Join(userHomeDir(), ".bmx", fmt.Sprintf("%s-%s", f.org, "sessions"))
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
