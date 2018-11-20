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
	printCmd.Flags().StringVar(&printOptions.Role, "role", "", "the desired role to assume")
	printCmd.Flags().BoolVar(&printOptions.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	if userConfig.Org == "" {
		printCmd.MarkFlagRequired("org")
	}

	rootCmd.AddCommand(printCmd)
}

var printCmd = &cobra.Command{
	Use:   "print",
	Short: "Print to screen",
	Long:  `Print the long stuff to screen`,
	Run: func(cmd *cobra.Command, args []string) {
		mergedOptions := mergePrintOptions(userConfig, printOptions)

		oktaClient, err := okta.NewOktaClient(mergedOptions.Org)
		if err != nil {
			log.Fatal(err)
		}

		bmx.Print(oktaClient, mergedOptions)
	},
}

func mergePrintOptions(uc config.UserConfig, pc bmx.PrintCmdOptions) bmx.PrintCmdOptions {
	if pc.Org == "" {
		pc.Org = uc.Org
	}
	if pc.User == "" {
		pc.User = uc.User
	}
	if pc.Account == "" {
		pc.Account = uc.Account
	}
	if pc.Role == "" {
		pc.Role = uc.Role
	}

	return pc
}
