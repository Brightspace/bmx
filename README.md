# BMX

## Install
```bash
git clone git@github.com:Brightspace/bmx.git
cd bmx
pip install -e .
bmx -h
```

## Usage
```
usage: bmx [-h] [--username USERNAME] {aws,write,print} ...

Okta time-out helper for AWS CLI

optional arguments:
  -h, --help           show this help message and exit
  --username USERNAME  specify username instead of being prompted

commands:
  {aws,write,print}
    aws                awscli with automatic STS token renewal
    write              write default credentials
    print              write default credentials and print to console

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
