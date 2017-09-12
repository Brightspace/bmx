[![Build Status](https://travis-ci.com/Brightspace/bmx.svg?token=XBuHJueJZM92zaxjesN6&branch=master)](https://travis-ci.com/Brightspace/bmx)

# BMX

BMX helps you keep your Okta-based AWS STS tokens fresh as you use the AWS CLI.  As you run AWS CLI commands, BMX watches each one.  When BMX sees a command fail due to an expired (or a missing) token,

1. it prompts for your Okta credentials and desired AWS account/role,
2. requests a new STS token from AWS,
3. writes the new AWS credentials to your AWS .credentials file, and
4. reruns the AWS CLI command.

[A BMX demo](https://internal.desire2learncapture.com/1/Watch/126176213250197203083009125230093245081043106145.aspx) is on Capture.

## Development

BMX is designed to be extensible and easily rolled out.

* It's a command-driven utility (think of Git, or the AWS CLI) where new commands can be added to the base system.
* It's on our private Artifactory repo and can be easily installed.

BMX is written in Python, like the AWS CLI.

* It introduces no new language dependencies.
* BMX can easily run in the same process as the AWS CLI, reducing overhead.

### Developer Setup

```bash
git clone git@github.com:Brightspace/bmx.git
cd bmx
pip install -e .
bmx -h
```

### Pylint

BMX uses [Pylint](https://www.pylint.org/) to enforce styling and run quality checkers.

**Install**: `pip install pylint`

**Lint**: `pylint bmx`

### Current development

An active PR will add two things:

1. Support for specifying your Okta username as an option.
1. A new command that prints your AWS credentials to the screen.

### Slated development

There is still [lots of work to do on BMX](https://github.com/Brightspace/bmx/issues)!

## System Requirements

* [Python 3.6+](https://www.python.org/downloads/)
* pip, the Python installer.

## Installation

BMX lives in D2L's Artifactory repository.  You need to tell pip about this repository when you install BMX.

### PowerShell

```
PS C:\Users\credekop> py -3 --version
Python 3.6.2

PS C:\Users\credekop> py -3 -m pip install --user --upgrade --extra-index-url https://d2lartifacts.artifactoryonline.com/d2lartifacts/api/pypi/pypi-local/simple bmx
```

## Usage

To use BMX, just prepend your AWS CLI calls with 'bmx '.  Example usage in Cygwin is below.

```bash
$ python3 --version
Python 3.6.2

$ bmx -h

usage: bmx {aws,write,print} ...

Okta time-out helper for AWS CLI

commands:
  {aws,write,print}
    aws                awscli with automatic STS token renewal
    write              create new AWS credentials and write them to ~/.aws/credentials
    print              create new AWS credentials and print them to stdout

Copyright 2017 D2L Corporation
```

## Example
```
$ bmx aws cloudformation describe-stacks
{
    "Stacks": [
        {
...
        }
    ]
}

$ rm ~/.aws/credentials

$ bmx aws cloudformation describe-stacks
Your AWS STS token has expired.  Renewing...
Okta username: credekop
Okta password:

Available AWS Accounts:
 1: DEV-BroadcastEventService
 2: Dev-AnalyticsInegration
 3: Dev-BDP
 4: Dev-CI
 5: Dev-IPA-EDU
 6: Dev-LMS
 7: Dev-PD-Tools
 8: Dev-ServiceDashboard
 9: Dev-Staging
10: Dev-Translation
11: Lrn-NimbusToronto
12: PRD-BroadcastEventService
13: Prd-BDP
14: Prd-CDN
15: Prd-NA
16: Prd-ServiceDashboard
17: Prd-Totem
18: Service Dashboard
AWS Account Index: 11

Available Roles in Lrn-NimbusToronto:
 1: Lrn-NimbusToronto-Owner
 2: Lrn-NimbusToronto-User
Role Index: 2

{
    "Stacks": [
        {
...
        }
    ]
}
```
