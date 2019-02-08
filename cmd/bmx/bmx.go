package main

import (
	"fmt"
	"os"

	"github.com/Brightspace/bmx/config"

	"github.com/spf13/cobra"
)

var userConfig config.UserConfig

func init() {
	userConfig = (config.ConfigLoader{}).LoadConfigs()
}

func main() {
	if err := rootCmd.Execute(); err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
}

var rootCmd = &cobra.Command{}
