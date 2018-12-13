package main

import (
	"fmt"
	"os"

	"github.com/Brightspace/bmx/cmd/config"
	"github.com/Brightspace/bmx/cmd/console"
	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/io"
	"github.com/Brightspace/bmx/serviceProviders"

	"github.com/spf13/cobra"
)

var (
	consoleReadWriter io.ReadWriter
	identityClient    identityProviders.IdentityProvider
	serviceClient     serviceProviders.ServiceProvider
	userConfig        config.UserConfig
)

func init() {
	consoleReadWriter = console.NewReadWriter()
	userConfig = (config.ConfigLoader{}).LoadConfigs()

	printInit()
	writeInit()
}

var rootCmd = &cobra.Command{}

func Execute() {
	if err := rootCmd.Execute(); err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
}
