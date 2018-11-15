package bmx

import (
	"log"
	"os"
	"runtime"

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
	// creds := &sts.Credentials{
	// 	AccessKeyId:     aws.String("abc"),
	// 	SecretAccessKey: aws.String("key"),
	// 	SessionToken:    aws.String("token"),
	// }
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
