package main

import (
	"log"

	"github.com/Brightspace/bmx/config"
	"github.com/Brightspace/bmx/saml/identityProviders/okta"

	"github.com/Brightspace/bmx"
	"github.com/spf13/cobra"
)

var writeOptions = bmx.WriteCmdOptions{}

func init() {
	writeCmd.Flags().StringVar(&writeOptions.Org, "org", "", "the okta org api to target")
	writeCmd.Flags().StringVar(&writeOptions.Profile, "profile", "", "aws profile name")
	writeCmd.Flags().StringVar(&writeOptions.User, "user", "", "the user to authenticate with")
	writeCmd.Flags().StringVar(&writeOptions.Account, "account", "", "the account name to auth against")
	writeCmd.Flags().StringVar(&writeOptions.Output, "output", "", "write to the specified file instead of ~/.aws/credentials")
	writeCmd.Flags().BoolVar(&writeOptions.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	rootCmd.AddCommand(writeCmd)
}

var writeCmd = &cobra.Command{
	Use:   "write",
	Short: "Write to aws credential file",
	Long:  "Write to aws credential file",
	Run: func(cmd *cobra.Command, args []string) {
		MergeWriteCmdOptions(&writeOptions, config.DefaultConfig{})
		oktaClient, err := okta.NewOktaClient(writeOptions.Org)
		if err != nil {
			log.Fatal(err)
		}

		bmx.Write(oktaClient, writeOptions)
	},
}

func MergeWriteCmdOptions(writeOptions *bmx.WriteCmdOptions, config Config) {
	cfg := config.LoadConfigs()
	if cfg != nil {
		if writeOptions.Org == "" {
			writeOptions.Org = cfg.Key("org").String()
			if writeOptions.Org == "" {
				log.Fatal(`Have to provide "Org" either in config file or command line argument`)
			}
		}
		if writeOptions.Profile == "" {
			writeOptions.Profile = cfg.Key("profile").String()
			if writeOptions.Profile == "" {
				log.Fatal(`Have to provide "Profile" either in config file or command line argument`)
			}
		}
		if writeOptions.User == "" {
			writeOptions.User = cfg.Key("user").String()
		}
		if writeOptions.Account == "" {
			writeOptions.Account = cfg.Key("account").String()
		}
	}
}
