# BMX

BMX provides API access to your AWS accounts using existing Okta credentials. Instead of long-term IAM user access keys, BMX creates short-term AWS STS tokens using your Okta identity.

## Installation

Download the appropriate binary from the [releases](https://github.com/Brightspace/bmx/releases) page. For D2Lers, visit [bmx.d2l.dev](https://bmx.d2l.dev) for installation.

## Commands

### configure

Set up the global BMX configuration file with the following command:
```PowerShell
bmx configure --org okta_organization --user okta_username
```
### print

To set up AWS credentials as environment variables in PowerShell, run:
```PowerShell
bmx print --account aws_account_name --role aws_role_name | iex
```
Or in Bash/sh/Zsh, run:
```Bash
eval "$(bmx print --account aws_account_name --role aws_role_name)"
```

### write

Create a new AWS credentials profile with the following command:
```Powershell
bmx write -- aws__name --role aws_role_name --profile aws_profile
```
You can use your created profile by configuring any supporting AWS client. For example, for the AWS CLI :
```Powershell
aws sts get-caller-identity --profile aws_profile
```

### Notes

Okta user sessions are automatically cached when a global configuration file is present. As such, it is not recommended to run `bmx configure` or create a global configuration file on a machine with multiple users.

The flags provided in the examples are optional. Use `bmx -h` or `bmx {command-name} -h` to view all available options.

## Configuration

BMX supports configuration files where you can define default values for most BMX flags.

A global configuration file at `~/.bmx/config`, if created, applies to all BMX commands.
You probably want to set `org` and `user` in this file.
The `bmx configure` command can help you set this up.

BMX also supports local configuration files named `.bmx`, which override the values in the global configuration file,
and only affect BMX commands executed in the current directory or subdirectories.
When executed, BMX will search upwards from the current working directory until it finds a file named `.bmx`.
Here's an example of a typical `.bmx` file:

```ini
account = aws_account_name
role = aws_role_name
duration = 15
```


## Upgrading from BMX 2 to BMX 3

### Breaking changes

* The `--output` flag for `bmx print` is renamed to `--format`. The `--output` flag for `bmx write` remains unchanged.
* `bmx version` is removed and replaced with `bmx --version`.
* `default_duration` in config file is renamed to `duration`.
* `allow_project_config` is removed from the global config file. Local `.bmx` config files are always enabled now.

### New features

* New command `bmx configure` for setting global configs.
* New command `bmx login` to create a cached Okta session without getting AWS credentials.
* All flags are optional by default, and BMX will prompt for user input if required info isn't provided as command line flags.
* New flag `--non-interactive` that suppresses all interactive prompts. If required info isn't already provided as command line flags, BMX will exit with an error.
* Password input prompt displays entered characters as asterisks.
* When MFA is required and only one method is available, that method is automatically selected.
* Support for Mintty (hence Git Bash, Cygwin, and MSYS2).
* JSON output format `bmx print --output json`.

See [release](https://github.com/Brightspace/bmx/releases) for the full list of changes.
