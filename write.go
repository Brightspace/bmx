package bmx

import (
	"log"
	"os"
	"runtime"

	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/io"
	"github.com/Brightspace/bmx/serviceProviders"
	"github.com/aws/aws-sdk-go/service/sts"
	ini "gopkg.in/ini.v1"
)

func Write(
	rw io.ReadWriter,
	identity identityProviders.IdentityProvider,
	service serviceProviders.ServiceProvider,
	username string,
	noMask bool,
	filter string,
	account string,
	role string,
	profile string,
	output string,
) error {

	creds, err := getCredentials(
		rw,
		identity,
		service,
		username,
		noMask,
		filter,
		account,
		role,
	)
	if err != nil {
		log.Fatal(err)
	}

	return writeToFile(creds, profile, resolvePath(output))
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

func writeToFile(credentials *sts.Credentials, profile string, path string) error {
	os.OpenFile(path, os.O_RDONLY|os.O_CREATE, 0666)

	cfg, err := ini.Load(path)
	if err != nil {
		return err
	}

	cfg.Section(profile).Key("aws_access_key_id").SetValue(*credentials.AccessKeyId)
	cfg.Section(profile).Key("aws_secret_access_key").SetValue(*credentials.SecretAccessKey)
	cfg.Section(profile).Key("aws_session_token").SetValue(*credentials.SessionToken)

	return cfg.SaveTo(path)
}
