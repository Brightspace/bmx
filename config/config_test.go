package config_test

import (
	"io/ioutil"
	"os"
	"path"
	"path/filepath"
	"testing"

	"github.com/Brightspace/bmx/config"
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
	err = os.MkdirAll(nestedDir, os.ModeDir)
	if err != nil {
		t.Fatal(err)
	}

	err = os.MkdirAll(path.Join(tempUserDir, ".bmx"), os.ModeDir)
	userConfigFile := filepath.ToSlash(path.Join(tempUserDir, ".bmx", "config"))
	ioutil.WriteFile(userConfigFile, []byte("allow_project_configs=true"), os.ModePerm)

	configFile := filepath.ToSlash(path.Join(tempDir, ".bmx"))
	ioutil.WriteFile(configFile, []byte("org=abc123"), os.ModePerm)

	m := config.ConfigLoader{
		UserDirectory:    tempUserDir,
		WorkingDirectory: nestedDir,
	}

	config := m.LoadConfigs()
	assert.Equal(t, config.Org, "abc123")
}
