package main

import (
	"fmt"

	"github.com/spf13/cobra"
)

var version string

func init() {
	rootCmd.AddCommand(versionCmd)
}

var versionCmd = &cobra.Command{
	Use:   "version",
	Short: "Print BMX version and exit",
	Run: func(cmd *cobra.Command, args []string) {
		fmt.Printf("BMX version %s\n", version)
	},
}
