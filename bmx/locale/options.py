# Common Argument Parser Strings
BMX_ACCOUNT_HELP = 'the AWS account name to auth against'
BMX_COPYRIGHT = 'Copyright 2017 D2L Corporation'
BMX_DESCRIPTION = 'Okta time-out helper for AWS CLI'
BMX_DURATION_HELP = 'requested STS-token lease duration in seconds (default: 3600)'
BMX_ROLE_HELP = 'the AWS role name to auth as'
BMX_USERNAME_HELP = 'specify Okta username instead of being prompted'


# BMX AWS Argument Parser Strings
BMX_AWS_HELP = 'run the AWS CLI, with automatic renewal of AWS credentials'
BMX_AWS_USAGE = '''

bmx aws -h
bmx aws [--username USERNAME] [--account ACCOUNT] [--role ROLE] CLICOMMAND CLISUBCOMMAND ...
'''


# BMX PRINT Argument Parser Strings
BMX_PRINT_HELP = 'print AWS credentials to stdout'
BMX_PRINT_USAGE = '''

bmx print -h
bmx print [--username USERNAME] [--duration DURATION] [--account ACCOUNT] [--role ROLE] [-j | -b | -p]
'''

BMX_PRINT_JSON_HELP = 'format credentials as JSON'
BMX_PRINT_BASH_HELP = 'format credentials for Bash'
BMX_PRINT_POWERSHELL_HELP = 'format credentials for PowerShell'


# BMX RENEW Argument Parser Strings
BMX_RENEW_HELP = 'renew AWS credentials'
BMX_RENEW_USAGE = '''

bmx renew -h
bmx renew [--username USERNAME]
          [--duration DURATION]
          [--account ACCOUNT]
          [--role ROLE]
'''


# BMX WRITE Argument Parser Strings
BMX_WRITE_HELP = 'write AWS credentials to ~/.aws/credentials'
BMX_WRITE_USAGE = '''

bmx write -h
bmx write [--username USERNAME]
          [--profile PROFILE]
          [--account ACCOUNT]
          [--role ROLE]'''

BMX_WRITE_PROFILE_HELP = 'the profile to write to the credentials file'

