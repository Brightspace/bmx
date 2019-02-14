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
	err = os.MkdirAll(nestedDir, os.ModeDir|os.ModePerm)
	if err != nil {
		t.Fatal(err)
	}

	err = os.MkdirAll(path.Join(tempUserDir, ".bmx"), os.ModeDir|os.ModePerm)
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
