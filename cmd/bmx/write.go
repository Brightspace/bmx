/*
Copyright 2019 D2L Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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
	writeCmd.Flags().StringVar(&writeOptions.Role, "role", "", "the desired role to assume")
	writeCmd.Flags().StringVar(&writeOptions.Output, "output", "", "write to the specified file instead of ~/.aws/credentials")
	writeCmd.Flags().BoolVar(&writeOptions.NoMask, "nomask", false, "set to not mask the password. this helps with debugging.")

	if userConfig.Org == "" {
		writeCmd.MarkFlagRequired("org")
	}

	if userConfig.Profile == "" {
		userConfig.Profile = "default"
	}

	rootCmd.AddCommand(writeCmd)
}

var writeCmd = &cobra.Command{
	Use:   "write",
	Short: "Write to aws credential file",
	Long:  "Write to aws credential file",
	Run: func(cmd *cobra.Command, args []string) {
		mergedOptions := MergeWriteCmdOptions(userConfig, writeOptions)
		oktaClient, err := okta.NewOktaClient(mergedOptions.Org)
		if err != nil {
			log.Fatal(err)
		}

		bmx.Write(oktaClient, mergedOptions)
	},
}

func MergeWriteCmdOptions(uc config.UserConfig, wc bmx.WriteCmdOptions) bmx.WriteCmdOptions {
	if wc.Org == "" {
		wc.Org = uc.Org
	}
	if wc.Profile == "" {
		wc.Profile = uc.Profile
	}
	if wc.User == "" {
		wc.User = uc.User
	}
	if wc.Account == "" {
		wc.Account = uc.Account
	}
	if wc.Role == "" {
		wc.Role = uc.Role
	}

	return wc
}
