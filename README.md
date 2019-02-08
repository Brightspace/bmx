[![Build Status](https://travis-ci.com/Brightspace/bmx.svg?token=XBuHJueJZM92zaxjesN6&branch=master)](https://travis-ci.com/Brightspace/bmx)
[![Coverage Status](https://coveralls.io/repos/github/Brightspace/bmx/badge.svg?branch=go&t=c1nzIP)](https://coveralls.io/github/Brightspace/bmx?branch=go)


# BMX

BMX grants you API access to your AWS accounts, based on Okta credentials that you already own.  
It uses your Okta identity to create short-term AWS STS tokens, as an alternative to long-term IAM access keys.
BMX manages your STS tokens with five commands:

1. `bmx print` writes your short-term tokens to `stdout` as AWS environment variables.  You can execute `bmx print`'s output to make the environment variables available to your shell.
1. `bmx write` writes your short-term tokens to `~/.aws/credentials`.

BMX prints detailed usage information when you run `bmx -h` or `bmx <cmd> -h`.

[A BMX demo](https://internal.desire2learncapture.com/1/Watch/6371.aspx) is on Capture. (note that this demo is of the previous Python version)

## Installation

Available versions of BMX are available on the [releases](https://github.com/Brightspace/bmx/releases) page. 

## Features
1. BMX is multi-platform: it runs on Linux, Windows, and Mac.
1. BMX maintains your Okta session for 12 hours: you enter your Okta password once a day, and BMX takes care of the rest.
1. Project scoped configurations
1. BMX supports Web and SMS MFA.

## Configuration Files
Many of the commandline parameters for BMX can be specified in a configuration file located at `~/.bmx/config`. BMX will
load this file automatically and populate the parameters where appropriate.

### Configuration Parameters
* allow_project_configs (default=false) : Setting this to true will enable the project scoped configuration feature described below.
* org : Specify the Okta org to connect to here. This value sets the api base URL for Okta calls (https://{org}.okta.com/).
* user : This is the username used when connecting to the identity provider.
* account : The AWS account to retrieve credentials for.
* role : The AWS role to assume.
* profile : The profile to `write` in `~/.aws/credentials`.

### Project Scoped Configurations
A project configuration scope can be defined by creating a `.bmx` file anywhere in your project's directory structure. 
When running BMX in the folder with a `.bmx` file or in any folder nested beneath a `.bmx` file, BMX will walk up the 
hierarchy until it finds a `.bmx` file and overlay the configuration with the user scoped configuration file `~/.bmx/config`. 
Note that you must enable this feature with `allow_project_configs=true` in the user configuration file.

## Development

BMX is designed to be extensible and easily rolled out.

* BMX is written in [Go](https://golang.org)
* It's a command-driven utility (think of Git, Terraform, or the AWS CLI) where new commands can be added to the base system.

### Developer Setup

```sh
go get github.com/Brightspace/bmx
# until the 'go' branch is merged in to master:
cd ~/go/src/github.com/Brightspace/bmx
git checkout go
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