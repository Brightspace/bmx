[![Build Status](https://travis-ci.com/Brightspace/bmx.svg?token=XBuHJueJZM92zaxjesN6&branch=go)](https://travis-ci.com/Brightspace/bmx)

# BMX

BMX grants you API access to your AWS accounts, based on Okta credentials that you already own.  It uses your Okta identity to create short-term AWS STS tokens, as an alternative to long-term IAM access keys.  BMX manages your STS tokens with five commands:

1. `bmx print` writes your short-term tokens to `stdout` as AWS environment variables.  You can execute `bmx print`'s output to make the environment variables available to your shell.
1. `bmx write` writes your short-term tokens to `~/.aws/credentials`.

BMX prints detailed usage information when you run `bmx -h` or `bmx <cmd> -h`.

[A BMX demo](https://internal.desire2learncapture.com/1/Watch/6371.aspx) is on Capture.

## Installation

Available versions of BMX are available on the [releases](https://github.com/Brightspace/bmx/releases) page. 

## Features
1. BMX is multi-platform: it runs on Linux, Windows, and Mac.
1. BMX maintains your Okta session for 12 hours: you enter your Okta password once a day, and BMX takes care of the rest.
1. BMX supports Web and SMS MFA.

## Development

BMX is designed to be extensible and easily rolled out.

* BMX is written in [Go](https://golang.org)
* It's a command-driven utility (think of Git, Terraform, or the AWS CLI) where new commands can be added to the base system.

### Developer Setup

```
go get github.com/Brightspace/bmx/cmd/bmx
```

### Slated development

BMX has [issues](https://github.com/Brightspace/bmx/issues).

## Usage Examples

### Getting Help

```bash
$ bmx -h

Usage:
   [command]

Available Commands:
  help        Help about any command
  print       Print to screen
  write       Write to aws credential file

Flags:
  -h, --help   help for this command

Use " [command] --help" for more information about a command.
```