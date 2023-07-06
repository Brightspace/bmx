# BMX

BMX provides API access to your AWS accounts using existing Okta credentials. Instead of long-term IAM access keys, BMX creates short-term AWS STS tokens using your Okta identity.

# Installation

Download the appropriate binary from the [releases](https://github.com/Brightspace/bmx/releases) page.

# Commands

## configure

Set up the global BMX configuration file with the following command:
```PowerShell
bmx configure --org your-okta-organization --user your-okta-username
```
BMX will default to use the provided values. Okta account sessions are also saved when a configuration file is present. As such, it is not recommended to run `bmx configure` or create a configuration file on a machine with multiple users.

Note: The flags provided are optional. Use `bmx configure -h` to view all available options.
## print

To setup AWS credentials in PowerShell, use:
```PowerShell
bmx print --account account-name --role role-name | iex
```
To setup AWS credentials in Bash, use:
```Bash
$(bmx print --account account-name --role role-name)
```
Note: The flags provided are optional. Use `bmx print -h` to view all available options.

## write

Create a new AWS credentials profile with the following command:
```Powershell
bmx write --account account-name --role role-name --profile my-profile
```
You can use your created profile by providing the `profile` flag in your AWS CLI commands, for example:
```Powershell
aws sts get-caller-identity --profile my-profile
```
Note: The flags provided are optional. Use `bmx write -h` to view all available options.


# Project-Level Configuration Files

BMX supports project-specific `.bmx` configuration files, which allow you to pre-select AWS account and name roles for user with the `bmx print` command. When executed, BMX will search upwards from the current working directory until it finds a configuration file.

Here's an example of a `.bmx` file:
```
account=account-name
role=role-name
duration=15
```
