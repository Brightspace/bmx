import configparser
import os
import argparse

import bmx.credentialsutil as credentialsutil
import bmx.stsutil as stsutil

from bmx.locale.options import (BMX_WRITE_USAGE, BMX_WRITE_PROFILE_HELP,
                                BMX_ACCOUNT_HELP, BMX_ROLE_HELP, BMX_USERNAME_HELP)

def create_parser():
    parser = argparse.ArgumentParser(prog='bmx write', usage=BMX_WRITE_USAGE)

    parser.add_argument('--username', default=None, help=BMX_USERNAME_HELP)
    parser.add_argument('--profile', default='default', help=BMX_WRITE_PROFILE_HELP)
    parser.add_argument('--account', default=None, help=BMX_ACCOUNT_HELP)
    parser.add_argument('--role', default=None, help=BMX_ROLE_HELP)

    return parser

def get_aws_path():
    return os.path.join(os.path.expanduser('~'), '.aws')

def get_credentials_path():
    return os.path.join(get_aws_path(), 'credentials')

def write_credentials(aws_credentials, profile):
    config = configparser.ConfigParser()

    config.read(get_credentials_path())
    config[profile] = {
        'aws_access_key_id': aws_credentials.keys['AccessKeyId'],
        'aws_secret_access_key': aws_credentials.keys['SecretAccessKey'],
        'aws_session_token': aws_credentials.keys['SessionToken']
    }

    with open(get_credentials_path(), 'w') as config_file:
        config.write(config_file)

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    bmx_credentials = credentialsutil.load_bmx_credentials()
    aws_credentials = bmx_credentials.get_credentials(
            app=known_args.account, role=known_args.role)

    if not aws_credentials:
        print('Requesting a token from AWS STS...')
        aws_credentials = stsutil.get_credentials(
                known_args.username, 3600, known_args.account, known_args.role)

    write_credentials(aws_credentials, known_args.profile)

    bmx_credentials.put_credentials(aws_credentials)
    bmx_credentials.write()

    return 0
