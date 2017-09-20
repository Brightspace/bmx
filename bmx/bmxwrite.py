import configparser
import os
import argparse

import bmx
import bmx.credentialsutil as credentialsutil
from bmx.locale.options import (BMX_WRITE_USAGE, BMX_WRITE_PROFILE_HELP,
                                BMX_ACCOUNT_HELP, BMX_ROLE_HELP, BMX_USERNAME_HELP)

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx write',
        usage=BMX_WRITE_USAGE
    )
    parser.add_argument('--username',
        help=BMX_USERNAME_HELP)

    parser.add_argument(
        '--profile',
        default='default',
        help=BMX_WRITE_PROFILE_HELP)

    parser.add_argument('--account', default=None, help=BMX_ACCOUNT_HELP)

    parser.add_argument('--role', default=None, help=BMX_ROLE_HELP)

    return parser

def get_aws_path():
    return os.path.join(os.path.expanduser('~'), '.aws')

def get_credentials_path():
    return os.path.join(get_aws_path(), 'credentials')

def write_credentials(credentials, profile):
    config = configparser.ConfigParser()

    config.read(get_credentials_path())
    config[profile] = {
        'aws_access_key_id': credentials.keys['AccessKeyId'],
        'aws_secret_access_key': credentials.keys['SecretAccessKey'],
        'aws_session_token': credentials.keys['SessionToken']
    }

    with open(get_credentials_path(), 'w') as config_file:
        config.write(config_file)

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    credentials = bmx.fetch_credentials(
            known_args.username, app=known_args.account, role=known_args.role)

    write_credentials(credentials, known_args.profile)

    credentialsutil.write_credentials(credentials)

    return 0
