package cmd

import (
	"fmt"
	"os"

	"github.com/Brightspace/bmx/cli/console"

	"github.com/spf13/cobra"
)

var consoleReader console.Reader

func init() {
	consoleReader = console.Reader{}
}

var rootCmd = &cobra.Command{
	Use:   "bmx",
	Short: "bmx does the things",
	Long: `A long thing that
        bmx does`,
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Println("do stuff?")
	},
}

func Execute() {
	if err := rootCmd.Execute(); err != nil {
		fmt.Println(err)
		os.Exit(1)
	}
}
