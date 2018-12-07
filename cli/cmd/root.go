package cmd

import (
	"fmt"
	"os"

	"github.com/Brightspace/bmx/cli/config"
	"github.com/Brightspace/bmx/cli/console"

	"github.com/spf13/cobra"
)

var consoleReader console.Reader
var userConfig config.UserConfig

func init() {
	consoleReader = console.Reader{}
	userConfig = (config.ConfigLoader{}).LoadConfigs()
}

var rootCmd = &cobra.Command{}

func Execute() {
	if err := rootCmd.Execute(); err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
}
