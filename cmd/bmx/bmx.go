package main

import (
	"log"
	"os"

	"github.com/Brightspace/bmx"
)

var options = bmx.Options{}

func main() {
	options.Command = os.Args[1]

	switch options.Command {
	case "print":
		if len(os.Args) > 2 {
			bmx.Print(options, os.Args[2:])
		} else {
			bmx.Print(options, []string{})
		}
	default:
		log.Fatalf("Unknown command: %s", options.Command)
	}
}
