package cmd

import (
	"fmt"
	"log"

	"github.com/toddradigan/bmx-go/bmx/identityProviders"
	"github.com/toddradigan/bmx-go/bmx/identityProviders/okta"
	"github.com/toddradigan/bmx-go/bmx/serviceProviders"
	"github.com/toddradigan/bmx-go/bmx/serviceProviders/aws"

	"github.com/aws/aws-sdk-go/service/sts"
	"github.com/spf13/cobra"
)

type printOptions struct {
	Org      string
	User     string
	Account  string
	NoMask   bool
	Password string
	Role     string
	Filter   string
}

var printOpts = printOptions{}

func init() {
	printCmd.Flags().StringVar(&printOpts.Org, "org", "d2l", "the okta org api to target")
	printCmd.Flags().StringVar(&printOpts.User, "user", "", "the user to authenticate with")
	printCmd.Flags().StringVar(&printOpts.Account, "account", "", "the account name to auth against")
	printCmd.Flags().StringVar(&printOpts.Role, "role", "", "the desired role to assume")
	printCmd.Flags().StringVar(&printOpts.Filter, "filter", "amazon_aws", "filter apps")
	printCmd.Flags().BoolVar(&printOpts.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	printCmd.MarkFlagRequired("user")

	rootCmd.AddCommand(printCmd)
}

var identityClient identityProviders.IdentityProvider
var serviceClient serviceProviders.ServiceProvider

var printCmd = &cobra.Command{
	Use:   "print",
	Short: "Print credentials",
	Run: func(cmd *cobra.Command, args []string) {
		var err error
		identityClient, err = okta.NewOktaClient(printOpts.Org)
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
			printOpts.User,
			printOpts.NoMask,
			printOpts.Filter,
			printOpts.Account,
			printOpts.Role,
		)
		if err != nil {
			log.Fatal(err)
		}

		printDefaultFormat(creds)
	},
}

func printPowershell(credentials *sts.Credentials) {
	fmt.Printf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) {
	fmt.Printf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s\n", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
