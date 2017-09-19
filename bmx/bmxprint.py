#!/usr/bin/python3

import sys
import json
import argparse

import bmx.credentialsutil as credentialsutil

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx print',
        usage='''
bmx print -h
bmx print [--username USERNAME] [--duration DURATION] [--account ACCOUNT] [--role ROLE] [-j | -b | -p]
'''
    )

    parser.add_argument('--username', help='the Okta username')
    parser.add_argument(
        '--duration',
        default=3600,
        help='the requested STS-token lease duration'
    )
    parser.add_argument('--account', default=None, help='the aws account name to auth against')
    parser.add_argument('--role', default=None, help='the aws role name to auth as')

    formatting_group = parser.add_mutually_exclusive_group()
    formatting_group.add_argument(
        '-j',
        help='format the credentials as JSON',
        action='store_true'
    )
    formatting_group.add_argument(
        '-b',
        help='format the credentials for Bash',
        action='store_true'
    )
    formatting_group.add_argument(
        '-p',
        help='format the credentials for PowerShell',
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
    credentials = credentialsutil.fetch_credentials(
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
