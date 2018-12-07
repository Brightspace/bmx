package config

import (
	"io/ioutil"
	"os"
	"path"
	"path/filepath"
	"testing"

	"github.com/magiconair/properties/assert"
)

func TestLoadConfigFile(t *testing.T) {
	tempUserDir, err := ioutil.TempDir("", "bmxtest_")
	if err != nil {
		t.Fatal(err)
	}
	defer os.RemoveAll(tempUserDir)

	tempDir, err := ioutil.TempDir("", "bmxtest_")
	if err != nil {
		t.Fatal(err)
	}
	defer os.RemoveAll(tempDir)

	nestedDir := filepath.ToSlash(path.Join(tempDir, "this", "is", "a", "nested", "project"))
	err = os.MkdirAll(nestedDir, os.ModeDir|os.ModePerm)
	if err != nil {
		t.Fatal(err)
	}

	err = os.MkdirAll(path.Join(tempUserDir, ".bmx"), os.ModeDir|os.ModePerm)
	userConfigFile := filepath.ToSlash(path.Join(tempUserDir, ".bmx", "config"))
	ioutil.WriteFile(userConfigFile, []byte("allow_project_configs=true"), os.ModePerm)

	configFile := filepath.ToSlash(path.Join(tempDir, ".bmx"))
	ioutil.WriteFile(configFile, []byte("org=abc123"), os.ModePerm)

	m := ConfigLoader{
		UserDirectory:    tempUserDir,
		WorkingDirectory: nestedDir,
	}

	cfg := m.LoadConfigs()
	assert.Equal(t, cfg.Org, "abc123")
}
