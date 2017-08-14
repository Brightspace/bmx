# BMX

BMX helps you keep your Okta-based AWS STS tokens fresh as you use the AWS CLI.  As you run AWS CLI commands, BMX watches each one.  When BMX sees a command fail due to an expired (or a missing) token,

1. it prompts for your Okta credentials and desired AWS account/role,
2. requests a new STS token from AWS,
3. writes the new AWS credentials to your AWS .credentials file, and
4. reruns the AWS CLI command.

See usage below for an example.

## System Requirements

* [Python 3.5+](https://www.python.org/downloads/windows/) (BMX uses [contextlib.redirect_stderr](https://docs.python.org/3/library/contextlib.html), which was introduced in version 3.5.)
* pip, the Python installer.
* [The AWS CLI](http://docs.aws.amazon.com/cli/latest/userguide/cli-chap-welcome.html) (BMX does not install the AWS CLI as a dependency; if you don't already have it, install the AWS CLI before proceeding.)

## Installation

BMX lives in D2L's Artifactory repository.  You need to tell pip about this repository when you install BMX; otherwise the installation is just like AWS CLI's.

### PowerShell

```
PS C:\Users\credekop> py -3 --version
Python 3.6.2

PS C:\Users\credekop> py -3 -m pip install --user --upgrade --extra-index-url https://d2lartifacts.artifactoryonline.com/d2lartifacts/api/pypi/pypi-local/simple bmx
```

## Usage

To use BMX, just prepend your AWS CLI calls with 'bmx '.  An example usage in Cygwin is below.

```bash
$ python3 --version
Python 3.6.2

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
