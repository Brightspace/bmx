# BMX

BMX provides API access to your AWS accounts using existing Okta credentials. Instead of long-term IAM access keys, BMX creates short-term AWS STS tokens using your Okta identity.

# Installation

Download the appropriate binary from the [releases](https://github.com/Brightspace/bmx/releases) page. Place the executable in a convenient location within your file system.

# Commands

## configure

The `bmx configure` command creates or updates the global BMX configuration file, located at `~/.bmx/config`. This file can store your Okta organization, username, and your AWS default session duration. Okta account sessions are saved when a configuration file is present. For security reasons, it's recommended to avoid running `bmx configure` or creating a global configuration file on a machine used by multiple users.

## print

The `bmx print` command returns your AWS credentials in format compatible with most command-line interfaces (CLIs). Common use cases include `bmx print | iex` for PowerShell and `$(bmx print)` for Bash/Terminal.

## write

The `bmx write` command saves your AWS credentials to the `~/.aws/credentials` file, or to another file of your choice.

# Project-Level Configuration Files

BMX allows you to create project-specific `.bmx` configuration files, enabling you to pre-select AWS account and role names for use with the `bmx print` command. When executed, BMX will search upwards from the current working directory until it locates a configuration file. This feature provides an efficient way to manage your AWS account settings on a per-project basis.

# Contributing

Please see our [Contributing Guidelines](https://github.com/Brightspace/bmx/blob/main/CONTRIBUTING.md) for more details.

# License

This project is licensed under the [Apache License 2.0](https://github.com/Brightspace/bmx/blob/main/LICENSE).
