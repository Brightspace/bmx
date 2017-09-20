#!/usr/bin/python3

import sys
import json
import argparse

import bmx
import bmx.credentialsutil as credentialsutil
from bmx.locale.options import (BMX_PRINT_USAGE,
                                BMX_PRINT_BASH_HELP,BMX_PRINT_JSON_HELP, BMX_PRINT_POWERSHELL_HELP,
                                BMX_ACCOUNT_HELP, BMX_DURATION_HELP, BMX_ROLE_HELP)

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx print',
        usage=BMX_PRINT_USAGE
    )

    parser.add_argument('--username', help='the Okta username')
    parser.add_argument(
        '--duration',
        default=3600,
        help=BMX_DURATION_HELP
    )
    parser.add_argument('--account', default=None, help=BMX_ACCOUNT_HELP)
    parser.add_argument('--role', default=None, help=BMX_ROLE_HELP)

    formatting_group = parser.add_mutually_exclusive_group()
    formatting_group.add_argument(
        '-j',
        help=BMX_PRINT_JSON_HELP,
        action='store_true'
    )
    formatting_group.add_argument(
        '-b',
        help=BMX_PRINT_BASH_HELP,
        action='store_true'
    )
    formatting_group.add_argument(
        '-p',
        help=BMX_PRINT_POWERSHELL_HELP,
        action='store_true'
    )

    return parser

def json_format_credentials(credentials):
    return json.dumps(
        {
            'AccessKeyId': credentials['AccessKeyId'],
            'SecretAccessKey': credentials['SecretAccessKey'],
            'SessionToken': credentials['SessionToken']
        },
        indent=4
    )

def bash_format_credentials(credentials):
    return """export AWS_ACCESS_KEY_ID='{}'
export AWS_SECRET_ACCESS_KEY='{}'
export AWS_SESSION_TOKEN='{}'""".format(
    credentials['AccessKeyId'],
    credentials['SecretAccessKey'],
    credentials['SessionToken']
)

def powershell_format_credentials(credentials):
    return """$env:AWS_ACCESS_KEY_ID = '{}';
$env:AWS_SECRET_ACCESS_KEY = '{}';
$env:AWS_SESSION_TOKEN = '{}'""".format(
    credentials['AccessKeyId'],
    credentials['SecretAccessKey'],
    credentials['SessionToken']
)

def format_credentials(args, credentials):
    aws_keys = credentials.keys

    if args.b:
        formatted_credentials = bash_format_credentials(aws_keys)
    elif args.p:
        formatted_credentials = powershell_format_credentials(aws_keys)
    else:
        formatted_credentials = json_format_credentials(aws_keys)

    return formatted_credentials

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]
    credentials = bmx.fetch_credentials(
        username=known_args.username,
        duration_seconds=known_args.duration,
        app=known_args.account,
        role=known_args.role
    )

    print(format_credentials(known_args, credentials))
    credentialsutil.write_credentials(credentials)

    return 0

def main():
    sys.exit(cmd(sys.argv))
