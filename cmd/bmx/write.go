package main

import (
	"log"

	"github.com/Brightspace/bmx"
	"github.com/Brightspace/bmx/cmd/config"
	"github.com/Brightspace/bmx/identityProviders/okta"
	"github.com/Brightspace/bmx/serviceProviders/aws"

	"github.com/spf13/cobra"
)

type writeOptions struct {
	Org     string
	User    string
	Account string
	NoMask  bool
	Profile string
	Role    string
	Filter  string
	Output  string
}

var writeOpts = writeOptions{}

func writeInit() {
	writeCmd.Flags().StringVar(&writeOpts.Org, "org", "d2l", "the okta org api to target")
	writeCmd.Flags().StringVar(&writeOpts.Profile, "profile", "", "aws profile name")
	writeCmd.Flags().StringVar(&writeOpts.User, "user", "", "the user to authenticate with")
	writeCmd.Flags().StringVar(&writeOpts.Account, "account", "", "the account name to auth against")
	writeCmd.Flags().StringVar(&writeOpts.Role, "role", "", "the desired role to assume")
	writeCmd.Flags().StringVar(&writeOpts.Filter, "filter", "amazon_aws", "filter apps")
	writeCmd.Flags().StringVar(&writeOpts.Output, "output", "", "write to the specified file instead of ~/.aws/credentials")
	writeCmd.Flags().BoolVar(&writeOpts.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	if userConfig.Profile == "" {
		writeCmd.MarkFlagRequired("profile")
	}

	rootCmd.AddCommand(writeCmd)
}

var writeCmd = &cobra.Command{
	Use:   "write",
	Short: "Write to aws credential file",
	Run: func(cmd *cobra.Command, args []string) {

		opts := mergeWriteOpts(userConfig, writeOpts)

		var err error
		identityClient, err = okta.NewOktaClient(opts.Org)
		if err != nil {
			log.Fatal(err)
		}

		serviceClient, err = aws.NewAWSServiceProvider()
		if err != nil {
			log.Fatal(err)
		}

		if err := bmx.Write(
			consoleReadWriter,
			identityClient,
			serviceClient,
			opts.User,
			opts.NoMask,
			opts.Filter,
			opts.Account,
			opts.Role,
			opts.Profile,
			opts.Output,
		); err != nil {
			log.Fatal(err)
		}
	},
}

func mergeWriteOpts(cfg config.UserConfig, writeOpts writeOptions) writeOptions {
	mergedOpts := writeOptions{}

	mergedOpts.Org = cfg.Org
	mergedOpts.User = cfg.User
	mergedOpts.Account = cfg.Account
	mergedOpts.Role = cfg.Role
	mergedOpts.Filter = cfg.Filter
	mergedOpts.Profile = cfg.Profile
	mergedOpts.NoMask = writeOpts.NoMask

	if writeOpts.Org != "" {
		mergedOpts.Org = writeOpts.Org
	}

	if writeOpts.User != "" {
		mergedOpts.User = writeOpts.User
	}

	if writeOpts.Account != "" {
		mergedOpts.Account = writeOpts.Account
	}

	if writeOpts.Role != "" {
		mergedOpts.Role = writeOpts.Role
	}

	if writeOpts.Filter != "" {
		mergedOpts.Filter = writeOpts.Filter
	}

	if writeOpts.Profile != "" {
		mergedOpts.Profile = writeOpts.Profile
	}

	return mergedOpts
}
