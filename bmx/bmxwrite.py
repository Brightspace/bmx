#!/usr/bin/python3

import configparser
import sys
import os
import argparse

import bmx.stsutil as stsutil

def renew_credentials(username=None, profile='default', duration_seconds=3600, app=None, role=None):
    write_credentials(stsutil.get_credentials(username,
                                              duration_seconds,
                                              app=app,
                                              role=role), profile)

def write_credentials(credentials, profile):
    config = configparser.ConfigParser()
    filename = os.path.expanduser('~/.aws/credentials')

    config.read(filename)
    config[profile] = {
        'aws_access_key_id': credentials.keys['AccessKeyId'],
        'aws_secret_access_key': credentials.keys['SecretAccessKey'],
        'aws_session_token': credentials.keys['SessionToken']
    }

    with open(os.path.expanduser('~/.aws/credentials'), 'w') as config_file:
        config.write(config_file)

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx write',
        usage='''

bmx write -h
bmx write [--username USERNAME]
          [--duration DURATION]
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

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]
    renew_credentials(known_args.username, known_args.profile,
                      app=known_args.account, role=known_args.role)

    return 0

def main():
    sys.exit(cmd(sys.argv))
