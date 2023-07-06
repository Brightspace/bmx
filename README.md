# BMX

BMX provides API access to your AWS accounts using existing Okta credentials. Instead of long-term IAM access keys, BMX creates short-term AWS STS tokens using your Okta identity.

## Installation

Download the appropriate binary from the [releases](https://github.com/Brightspace/bmx/releases) page. For D2Lers, visit [bmx.d2l.dev](https://bmx.d2l.dev)

## Commands

### configure

Set up the global BMX configuration file with the following command:
```PowerShell
bmx configure --org your-okta-organization --user your-okta-username
```
### print

To setup AWS credentials as environment variables in PowerShell, run:
```PowerShell
bmx print --account account-name --role role-name | iex
```
Or in Bash/sh/Zsh, run:
```Bash
$(bmx print --account account-name --role role-name)
```

### write

Create a new AWS credentials profile with the following command:
```Powershell
bmx write --account account-name --role role-name --profile my-profile
```
You can use your created profile by configuring any supporting AWS client. For example, for the AWS CLI :
```Powershell
aws sts get-caller-identity --profile my-profile
```
### notes

Okta account sessions are also saved when a configuration file is present. As such, it is not recommended to run `bmx configure` or create a configuration file on a machine with multiple users.

The flags provided in the examples are optional. Use `bmx -h` or `bmx {command-name} -h` to view all available options.

## Project-Level Configuration Files

BMX supports project-specific `.bmx` configuration files, which allow you to define default values for most CLI flags. When executed, BMX will search upwards from the current working directory until it finds a configuration file.

Here's an example of a `.bmx` file:
```
account=account-name
role=role-name
duration=15
```
