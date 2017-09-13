#!/usr/bin/python3

import sys
import json
import argparse
import os
import configparser

import bmx.stsutil as stsutil

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx print',
        usage='''
        
bmx print -h
bmx print [--username USERNAME] [--duration DURATION] [--account ACCOUNT] [--role ROLE] [-j | -b | -p]
bmx print [--profile PROFILE] [-j | -b | -p]'''
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

    parser.add_argument(
        '--profile',
        help='reads an existing profile from the credentials file',
        default=''
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
    formatted_credentials = None

    if args.b:
        formatted_credentials = bash_format_credentials(credentials)
    elif args.p:
        formatted_credentials = powershell_format_credentials(credentials)
    else:
        formatted_credentials = json_format_credentials(credentials)

    return formatted_credentials

def read_config(profile):
    config = configparser.ConfigParser()
    filename = os.path.expanduser('~/.aws/credentials')

    config.read(filename)
    access_key_id = config.get(profile, 'aws_access_key_id')
    secret_access_key = config.get(profile, 'aws_secret_access_key')
    session_token = config.get(profile, 'aws_session_token')

    return {
        'AccessKeyId': access_key_id,
        'SecretAccessKey': secret_access_key,
        'SessionToken': session_token
    }

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    if known_args.profile:
        try:
            credentials = read_config(known_args.profile)
        except configparser.NoSectionError:
            return 'Profile not found'
    else:
        credentials = stsutil.get_credentials(
            known_args.username,
            known_args.duration,
            app=known_args.account,
            role=known_args.role
        )

    print(format_credentials(known_args, credentials))

    return 0

def main():
    sys.exit(cmd(sys.argv))
