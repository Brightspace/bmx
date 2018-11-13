package bmx

import (
	"fmt"
	"log"
	"os"
	"runtime"

	"github.com/aws/aws-sdk-go/service/sts"
)

type WriteCmdOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string
	Profile  string
}

func GetUserInfoFromWriteCmdOptions(writeOptions WriteCmdOptions) UserInfo {
	user := UserInfo{
		Org:      writeOptions.Org,
		User:     writeOptions.User,
		Account:  writeOptions.Account,
		NoMask:   writeOptions.NoMask,
		Password: writeOptions.Password,
	}

	return user
}

func Write(oktaClient oktaClient, writeOptions WriteCmdOptions) {
	creds := GetCredentials(oktaClient, GetUserInfoFromWriteCmdOptions(writeOptions))
	WriteToAwsCredentials(creds, writeOptions.Profile)
}

func UserHomeDir() string {
	if runtime.GOOS == "windows" {
		home := os.Getenv("HOMEDRIVE") + os.Getenv("HOMEPATH")
		if home == "" {
			home = os.Getenv("USERPROFILE")
		}
		return home
	}
	return os.Getenv("HOME")
}

func AWSCredentialsPath() string {
	path := UserHomeDir() + "/.aws/creds"
	return path
}

func WriteToAwsCredentials(credentials *sts.Credentials, profile string) {
	file, err := os.Create(AWSCredentialsPath())
	if err != nil {
		log.Fatal("Cannot create file", err)
	}

	defer file.Close()
	fmt.Fprintf(file, "[%s]\nAWS_SESSION_TOKEN=%s\nAWS_ACCESS_KEY_ID=%s\nAWS_SECRET_ACCESS_KEY=%s", profile, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
