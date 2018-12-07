package cmd

import (
	"fmt"
	"log"

	"github.com/Brightspace/bmx/cli/config"
	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/identityProviders/okta"
	"github.com/Brightspace/bmx/serviceProviders"
	"github.com/Brightspace/bmx/serviceProviders/aws"

	"github.com/aws/aws-sdk-go/service/sts"
	"github.com/spf13/cobra"
)

type printOptions struct {
	Org     string
	User    string
	Account string
	NoMask  bool
	Role    string
	Filter  string
}

var printOpts = printOptions{}

func init() {
	printCmd.Flags().StringVar(&printOpts.Org, "org", "d2l", "the okta org api to target")
	printCmd.Flags().StringVar(&printOpts.User, "user", "", "the user to authenticate with")
	printCmd.Flags().StringVar(&printOpts.Account, "account", "", "the account name to auth against")
	printCmd.Flags().StringVar(&printOpts.Role, "role", "", "the desired role to assume")
	printCmd.Flags().StringVar(&printOpts.Filter, "filter", "amazon_aws", "filter apps")
	printCmd.Flags().BoolVar(&printOpts.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	if userConfig.Org == "" {
		printCmd.MarkFlagRequired("user")
	}

	rootCmd.AddCommand(printCmd)
}

var identityClient identityProviders.IdentityProvider
var serviceClient serviceProviders.ServiceProvider

var printCmd = &cobra.Command{
	Use:   "print",
	Short: "Print credentials",
	Run: func(cmd *cobra.Command, args []string) {

		opts := mergePrintOpts(userConfig, printOpts)

		var err error
		identityClient, err = okta.NewOktaClient(opts.Org)
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
			opts.User,
			opts.NoMask,
			opts.Filter,
			opts.Account,
			opts.Role,
		)
		if err != nil {
			log.Fatal(err)
		}

		printDefaultFormat(creds)
	},
}

func mergePrintOpts(cfg config.UserConfig, printOpts printOptions) printOptions {
	mergedOpts := printOptions{}

	mergedOpts.Org = cfg.Org
	mergedOpts.User = cfg.User
	mergedOpts.Account = cfg.Account
	mergedOpts.Role = cfg.Role
	mergedOpts.Filter = cfg.Filter
	mergedOpts.NoMask = printOpts.NoMask

	if printOpts.Org != "" {
		mergedOpts.Org = printOpts.Org
	}

	if printOpts.User != "" {
		mergedOpts.User = printOpts.User
	}

	if printOpts.Account != "" {
		mergedOpts.Account = printOpts.Account
	}

	if printOpts.Role != "" {
		mergedOpts.Role = printOpts.Role
	}

	if printOpts.Filter != "" {
		mergedOpts.Filter = printOpts.Filter
	}

	return mergedOpts
}

func printPowershell(credentials *sts.Credentials) {
	fmt.Printf(`$env:AWS_SESSION_TOKEN='%s'; $env:AWS_ACCESS_KEY_ID='%s'; $env:AWS_SECRET_ACCESS_KEY='%s'`, *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}

func printBash(credentials *sts.Credentials) {
	fmt.Printf("export AWS_SESSION_TOKEN=%s\nexport AWS_ACCESS_KEY_ID=%s\nexport AWS_SECRET_ACCESS_KEY=%s\n", *credentials.SessionToken, *credentials.AccessKeyId, *credentials.SecretAccessKey)
}
