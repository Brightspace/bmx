import json
import argparse

import bmx.credentialsutil as credentialsutil
import bmx.stsutil as stsutil
from bmx.options import (BMX_PRINT_USAGE,
                     BMX_PRINT_BASH_HELP, BMX_PRINT_JSON_HELP, BMX_PRINT_POWERSHELL_HELP,
                     BMX_USERNAME_HELP, BMX_ACCOUNT_HELP, BMX_ROLE_HELP)


def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx print',
        usage=BMX_PRINT_USAGE
    )

    parser.add_argument('--username', help=BMX_USERNAME_HELP)
    parser.add_argument('--account', help=BMX_ACCOUNT_HELP)
    parser.add_argument('--role', help=BMX_ROLE_HELP)

    formatting_group = parser.add_mutually_exclusive_group()
    formatting_group.add_argument('-j', help=BMX_PRINT_JSON_HELP, action='store_true')
    formatting_group.add_argument('-b', help=BMX_PRINT_BASH_HELP, action='store_true')
    formatting_group.add_argument('-p', help=BMX_PRINT_POWERSHELL_HELP, action='store_true')

    return parser

def json_format_credentials(aws_keys):
    return json.dumps(
        {
            'AccessKeyId': aws_keys['AccessKeyId'],
            'SecretAccessKey': aws_keys['SecretAccessKey'],
            'SessionToken': aws_keys['SessionToken']
        },
        indent=4
    )

def bash_format_credentials(aws_keys):
    return """export AWS_ACCESS_KEY_ID='{}'
export AWS_SECRET_ACCESS_KEY='{}'
export AWS_SESSION_TOKEN='{}'""".format(
    aws_keys['AccessKeyId'],
    aws_keys['SecretAccessKey'],
    aws_keys['SessionToken']
)

def powershell_format_credentials(aws_keys):
    return """$env:AWS_ACCESS_KEY_ID = '{}';
$env:AWS_SECRET_ACCESS_KEY = '{}';
$env:AWS_SESSION_TOKEN = '{}'""".format(
    aws_keys['AccessKeyId'],
    aws_keys['SecretAccessKey'],
    aws_keys['SessionToken']
)

def format_credentials(args, aws_credentials):
    aws_keys = aws_credentials.keys

    if args.b:
        formatted_credentials = bash_format_credentials(aws_keys)
    elif args.p:
        formatted_credentials = powershell_format_credentials(aws_keys)
    else:
        formatted_credentials = json_format_credentials(aws_keys)

    return formatted_credentials

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]
    bmx_credentials = credentialsutil.load_bmx_credentials()
    aws_credentials = bmx_credentials.get_credentials(
            app=known_args.account, role=known_args.role)

    if not aws_credentials:
        aws_credentials = stsutil.get_credentials(
                known_args.username, 3600, known_args.account, known_args.role)

    print(format_credentials(known_args, aws_credentials))

    bmx_credentials.put_credentials(aws_credentials)
    bmx_credentials.write()

    return 0
