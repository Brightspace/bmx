import configparser
import sys
import os
import argparse

import bmx.credentialsutil as credentialsutil

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx write',
        usage='''

bmx write -h
bmx write [--username USERNAME]
          [--profile PROFILE]
          [--account ACCOUNT]
          [--role ROLE]'''
)
    parser.add_argument('--username',
        help='specify username instead of being prompted')

    parser.add_argument(
        '--profile',
        default='default',
        help='the profile to write to the credentials file')

    parser.add_argument('--account', default=None, help='the aws account name to auth against')

    parser.add_argument('--role', default=None, help='the aws role name to auth as')

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

    print('9999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999')

    credentials = credentialsutil.fetch_credentials(
            known_args.username, app=known_args.account, role=known_args.role)

    write_credentials(credentials, known_args.profile)

    credentialsutil.write_credentials(credentials)
     
    return 0
