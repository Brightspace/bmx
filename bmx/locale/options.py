# Common Argument Parser Strings
BMX_DESCRIPTION = 'IAM-less AWS API access for humans'
BMX_USERNAME_HELP = 'use the specified Okta username'
BMX_ACCOUNT_HELP = 'use the specified AWS account'
BMX_ROLE_HELP = 'use the specified AWS role'
BMX_COPYRIGHT = 'Copyright 2017 D2L Corporation'


# BMX AWS Argument Parser Strings
BMX_AWS_HELP = 'run the AWS CLI, updating AWS STS tokens as necessary'
BMX_AWS_USAGE = '''

bmx aws -h
bmx aws [--username USERNAME] [--account ACCOUNT] [--role ROLE] CLICOMMAND CLISUBCOMMAND ...
'''


# BMX PRINT Argument Parser Strings
BMX_PRINT_HELP = 'print an AWS STS token to stdout'
BMX_PRINT_USAGE = '''

bmx print -h
bmx print [--username USERNAME] [--account ACCOUNT] [--role ROLE] [-j | -b | -p]
'''

BMX_PRINT_JSON_HELP = 'format credentials as JSON'
BMX_PRINT_BASH_HELP = 'format credentials for Bash'
BMX_PRINT_POWERSHELL_HELP = 'format credentials for PowerShell'


# BMX REMOVE Argument Parser Strings
BMX_REMOVE_HELP = 'forget an AWS STS token'
BMX_REMOVE_USAGE = '''

bmx remove -h
bmx remove [--account ACCOUNT] [--role ROLE]
'''


# BMX RENEW Argument Parser Strings
BMX_RENEW_HELP = 'renew an AWS STS token'
BMX_RENEW_USAGE = '''

bmx renew -h
bmx renew [--username USERNAME] [--account ACCOUNT] [--role ROLE]
'''


# BMX WRITE Argument Parser Strings
BMX_WRITE_HELP = 'write an AWS STS token to ~/.aws/credentials'
BMX_WRITE_USAGE = '''

bmx write -h
bmx write [--username USERNAME] [--account ACCOUNT] [--role ROLE] [--profile PROFILE]'''

BMX_WRITE_PROFILE_HELP = 'use the specified profile in ~/.aws/credentials'
