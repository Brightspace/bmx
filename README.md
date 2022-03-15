# BMX
BMX grants you API access to your AWS accounts, based on Okta credentials that you already own.  
It uses your Okta identity to create short-term AWS STS tokens, as an alternative to long-term IAM access keys.
BMX manages your STS tokens with the following commands:

1. `bmx print` writes your short-term tokens to `stdout` as AWS environment variables.  You can execute `bmx print`'s output to make the environment variables available to your shell.
1. `bmx write` writes your short-term tokens to `~/.aws/credentials`.

BMX prints detailed usage information when you run `bmx -h` or `bmx <cmd> -h`.

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

## Versioning
BMX is maintained under the [Semantic Versioning guidelines](http://semver.org/).

## Development

BMX is designed to be extensible and easily rolled out.

* BMX is written in [Go](https://golang.org) and compiles into a single binary for distribution purposes
	* It makes use of [Go modules](https://github.com/golang/go/wiki/Modules)
	* Dependencies are [vendored](https://tip.golang.org/cmd/go/#hdr-Modules_and_vendoring) and everything is included in this repository to build locally 
* BMX is a command-driven utility (think of Git, Terraform, or the AWS CLI) leveraging the [cobra](https://github.com/spf13/cobra) library. New commands can be added to the base system with relative ease.

### Developer Setup

```sh
go get github.com/Brightspace/bmx
```

### Building
```bash
go build github.com/Brightspace/bmx/cmd/bmx
```


### Getting Involved

BMX has [issues](https://github.com/Brightspace/bmx/issues).

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Usage Examples

### Getting Help

```bash
$ bmx -h

Usage:
   [command]

Available Commands:
  help        Help about any command
  print       Print to screen
  version     Print BMX version and exit
  write       Write to aws credential file

Flags:
  -h, --help   help for this command

Use " [command] --help" for more information about a command.
```

### Sample ~/.bmx/config
```
allow_project_configs=true
org=my_okta_org
user=my_user
```

## How to use bmx and the AWS cli 🚲 - Opinionated Workflow

### Pre-requisites
- [Install the aws cli](https://aws.amazon.com/cli/)
- Install and configure the d2l bmx tool
  - [Guide on how to install bmx](https://github.com/Brightspace/bmx/wiki/Quick-Start)


### Step 1 - Add this alias to your bashrc

```bash
# bashrc
export OKTA_USER={YOUR_OKTA-USER_HERE}
alias bmxAwsAdmin='
    export AWS_PROFILE=dev-profile
    bmx write --org my_okta_org --user $OKTA_USER --account YOUR-ACCOUNT-HERE --role YOUR-ROLE-HERE --profile dev-profile'
alias bmxAwsReadOnly='
    export AWS_PROFILE=dev-profile
    bmx write --org my_okta_org --user $OKTA_USER --account YOUR-ACCOUNT-HERE --role YOUR-ROLE-HERE-ReadOnly --profile dev-profile'
```

Note: Be sure to restart your shell or run `source ~/.bashrc` to make the alias available in the shell
Note: Be sure to replace `--account` and `--role` flags with the account and role you want to have credentials for

Alternatively, the `--org` and `--user` flags above can be dropped if you prefer to define those values in the `~/.bmx/config` file. See [Recommended configuration file](https://github.com/Brightspace/bmx/wiki/Quick-Start#recommended-configuration-file) 


#### What does this do?

- Sets the aws profile env variable to the profile bmx is about to create
- runs bmx to log in using okta, then retrieve credentials and write them to the credentials file in `~/.aws/credentials`

You can verfiy this was done correctly by removing the credentials file and confirming the command creates and populates the file with the profile `dev-profile`.
```bash
rm -f ~/.aws/credentials
cat ~/.aws/credentials
bmxAwsReadOnly
cat ~/.aws/credentials

```

### Step 2 - Setting the default region

There are two ways to get default region when using the aws cli.
1. Set the environment variable `export AWS_DEFAULT_REGION={YOUR_DESIRED_REGION}`
2. run `aws configure`
   1. hit enter when prompted for Key ID
   2. hit enter when prompted for Access Key
   3. **Enter region name** and hit enter (i.e. `us-east-1`)
   4. hit enter one last time

Note: Option 2 will write the default value set to the `~/.aws/config` file. This value will now be the default every time you connect with that profile. This can be changed by running 2 once more or removed by deleting the config file.

Note: Environment variables will take precendence over any settings in config files.


### Step 3 - Confirm things are working properly

There are a few useful checks you can do to make sure things are working properly.

#### Check the configured values
This can be done by running
```bash
aws configure list
#       Name                    Value             Type    Location
#       ----                    -----             ----    --------
#    profile                  dev-profile              env    ['AWS_PROFILE', 'AWS_DEFAULT_PROFILE']
# access_key     ****************O7GY shared-credentials-file
# secret_key     ****************1ch7 shared-credentials-file
#     region                us-east-1              env    ['AWS_REGION', 'AWS_DEFAULT_REGION']
```

#### Run a basic AWS cli command

You can list all IAM users available with the following:

```bash
aws iam list-users
# {
#     "Users": [
#         {
#             "Path": "/",
#             "UserName": "Okta",
#             "UserId": "mEq2N2JQvzMUG44ZJDQQ",
#             "Arn": "arn:aws:iam::123123123123:user/Okta",
#             "CreateDate": "2018-07-09T19:40:10+00:00"
#         }
#     ]
# }

aws sts get-caller-identity
# {
#     "UserId": "mEq2N2JQvzMUG44ZJDQQ:oktauser@domain.com",
#     "Account": "123123123123",
#     "Arn": "arn:aws:sts::123123123123:assumed-role/YOUR-ROLE-HERE/oktauser@domain.com"
# }
```

If the profile is not properly setup you may see an error like this...

```bash
aws iam list-users

# Unable to locate credentials. You can configure credentials by running "aws configure".

```

