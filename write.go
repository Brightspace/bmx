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

package bmx

import (
	"log"
	"os"
	"runtime"

	"github.com/Brightspace/bmx/saml/identityProviders"
	"github.com/Brightspace/bmx/saml/serviceProviders"
	"github.com/aws/aws-sdk-go/service/sts"
	"gopkg.in/ini.v1"
)

type WriteCmdOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string
	Profile  string
	Output   string
	Role     string
	Duration int

	Provider serviceProviders.ServiceProvider
}

func GetUserInfoFromWriteCmdOptions(writeOptions WriteCmdOptions) serviceProviders.UserInfo {
	user := serviceProviders.UserInfo{
		Org:      writeOptions.Org,
		User:     writeOptions.User,
		Account:  writeOptions.Account,
		NoMask:   writeOptions.NoMask,
		Password: writeOptions.Password,
	}
	return user
}

func Write(idProvider identityProviders.IdentityProvider, writeOptions WriteCmdOptions) {
	writeOptions.User = getUserIfEmpty(writeOptions.User)
	user := GetUserInfoFromWriteCmdOptions(writeOptions)

	saml, err := authenticate(user, idProvider)
	if err != nil {
		log.Fatal(err)
	}

	creds := AwsServiceProvider.GetCredentials(saml, writeOptions.Role, writeOptions.Duration)
	writeToAwsCredentials(creds, writeOptions.Profile, resolvePath(writeOptions.Output))
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

func awsCredentialsPath() string {
	path := userHomeDir() + "/.aws/credentials"
	return path
}

func resolvePath(path string) string {
	if path == "" {
		path = awsCredentialsPath()
	}
	return path
}

func writeToAwsCredentials(credentials *sts.Credentials, profile string, path string) {
	os.OpenFile(path, os.O_RDONLY|os.O_CREATE, 0666)

	cfg, err := ini.Load(path)
	if err != nil {
		log.Fatal(err)
	}
	cfg.Section(profile).Key("aws_access_key_id").SetValue(*credentials.AccessKeyId)
	cfg.Section(profile).Key("aws_secret_access_key").SetValue(*credentials.SecretAccessKey)
	cfg.Section(profile).Key("aws_session_token").SetValue(*credentials.SessionToken)
	cfg.SaveTo(path)
}
