package cmd

import (
	"log"
	"os"
	"runtime"

	"github.com/toddradigan/bmx-go/bmx/identityProviders/okta"
	"github.com/toddradigan/bmx-go/bmx/serviceProviders/aws"

	"github.com/aws/aws-sdk-go/service/sts"
	"github.com/spf13/cobra"
	"gopkg.in/ini.v1"
)

type writeOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string
	Profile  string
	Role     string
	Filter   string
	Output   string
}

var writeOpts = writeOptions{}

func init() {
	writeCmd.Flags().StringVar(&writeOpts.Org, "org", "d2l", "the okta org api to target")
	writeCmd.Flags().StringVar(&writeOpts.Profile, "profile", "", "aws profile name")
	writeCmd.Flags().StringVar(&writeOpts.User, "user", "", "the user to authenticate with")
	writeCmd.Flags().StringVar(&writeOpts.Account, "account", "", "the account name to auth against")
	writeCmd.Flags().StringVar(&writeOpts.Role, "role", "", "the desired role to assume")
	writeCmd.Flags().StringVar(&writeOpts.Filter, "filter", "amazon_aws", "filter apps")
	writeCmd.Flags().StringVar(&writeOpts.Output, "output", "", "write to the specified file instead of ~/.aws/credentials")
	writeCmd.Flags().BoolVar(&writeOpts.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	writeCmd.MarkFlagRequired("user")
	writeCmd.MarkFlagRequired("profile")

	rootCmd.AddCommand(writeCmd)
}

var writeCmd = &cobra.Command{
	Use:   "write",
	Short: "Write to aws credential file",
	Run: func(cmd *cobra.Command, args []string) {
		var err error
		identityClient, err = okta.NewOktaClient(writeOpts.Org)
		if err != nil {
			log.Fatal(err)
		}

		serviceClient, err = aws.NewAWSServiceProvider()
		if err != nil {
			log.Fatal(err)
		}

		creds, err := getCredentials(
			consoleReader,
			identityClient,
			serviceClient,
			writeOpts.User,
			writeOpts.NoMask,
			writeOpts.Filter,
			writeOpts.Account,
			writeOpts.Role,
		)
		if err != nil {
			log.Fatal(err)
		}

		writeToFile(creds, writeOpts.Profile, resolvePath(writeOpts.Output))
	},
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

func writeToFile(credentials *sts.Credentials, profile string, path string) {
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
