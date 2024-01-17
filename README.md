# BMX

BMX provides API access to your AWS accounts using existing Okta credentials. Instead of long-term IAM user access keys, BMX creates short-term AWS STS tokens using your Okta identity.

## Installation

Download the appropriate binary from the [releases](https://github.com/Brightspace/bmx/releases) page. For D2Lers, visit [bmx.d2l.dev](https://bmx.d2l.dev) for installation.


## Usage

### Flags

BMX command line flags (a.k.a. "options", i.e. command line arguments that start with `--`) are optional unless otherwise stated.
If not provided, BMX will prompt you to input the data interactively as needed.

### Global configuration

To set up the BMX global configuration file at `~/.bmx/config`, run

```PowerShell
bmx configure --org <okta_organization> --user <okta_username>
```

Okta user sessions are automatically cached when this configuration file is present.
As such, it is not recommended to run `bmx configure` or create this configuration file manually on a machine with shared access.

### AWS credentials as environment variables

To set up AWS credentials as environment variables, in PowerShell, run

```PowerShell
bmx print --account <aws_account_name> --role <aws_role_name> | iex
```

or in Bash/sh/Zsh, run

```Bash
eval "$(bmx print --account <aws_account_name> --role <aws_role_name>)"
```

### Static AWS credentials in a profile

To set up AWS credentials in a profile, run

```Powershell
bmx write --account <aws_account_name> --role <aws_role_name> --profile <aws_profile>
```

You can use your profile by configuring any supporting AWS client. For example, for the AWS CLI:

```Powershell
aws sts get-caller-identity --profile <aws_profile>
```

### Provide dynamic AWS credentials to a profile

To set up an AWS profile that sources credentials from BMX on-the-fly, run

```Powershell
bmx write --use-credential-process --account <aws_account_name> --role <aws_role_name> --profile <aws_profile>
```

(_Note: the `--use-credential-process` flag must be provided on the command line._)

AWS clients using this profile will call BMX to obtain credentials on-the-fly.
BMX caches the credentials it provides, and will automatically refresh them as needed, as long as it has a valid Okta session.

This use case is only supported when you have set up the BMX global configuration file.

### Refresh Okta session

To force refresh your Okta session, run

```Powershell
bmx login
```

### Local configuration

You can create local configuration files named `.bmx`, where you can define default values for most BMX flags.
A local configuration file takes effect for BMX commands executed in the current directory or its subdirectories.
Its values override the values in the global configuration file.

Here's an example of a typical `.bmx` file:

```ini
account = <aws_account_name>
role = <aws_role_name>
duration = 15
```
