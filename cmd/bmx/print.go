package main

import (
	"log"

	"github.com/Brightspace/bmx/config"
	"github.com/Brightspace/bmx/saml/identityProviders/okta"

	"github.com/Brightspace/bmx"
	"github.com/spf13/cobra"
)

var printOptions = bmx.PrintCmdOptions{}

func init() {
	printCmd.Flags().StringVar(&printOptions.Org, "org", "", "the okta org api to target")
	printCmd.Flags().StringVar(&printOptions.User, "user", "", "the user to authenticate with")
	printCmd.Flags().StringVar(&printOptions.Account, "account", "", "the account name to auth against")
	printCmd.Flags().BoolVar(&printOptions.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	rootCmd.AddCommand(printCmd)
}

var printCmd = &cobra.Command{
	Use:   "print",
	Short: "Print to screen",
	Long:  `Print the long stuff to screen`,
	Run: func(cmd *cobra.Command, args []string) {
		MergePrintCmdOptions(&printOptions, config.DefaultConfig{})
		oktaClient, err := okta.NewOktaClient(printOptions.Org)
		if err != nil {
			log.Fatal(err)
		}

		bmx.Print(oktaClient, printOptions)
	},
}

func MergePrintCmdOptions(printOptions *bmx.PrintCmdOptions, config Config) {
	cfg := config.LoadConfigs()
	if cfg != nil {
		if printOptions.Org == "" {
			printOptions.Org = cfg.Key("org").String()
			if printOptions.Org == "" {
				log.Fatal(`Have to provide "Org" either in config file or command line argument`)
			}
		}
		if printOptions.User == "" {
			printOptions.User = cfg.Key("user").String()
		}
		if printOptions.Account == "" {
			printOptions.Account = cfg.Key("account").String()
		}
	}
}
