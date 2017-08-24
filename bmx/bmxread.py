#!/usr/bin/python3

import sys
import os
import argparse
import configparser

def cmd(args):
    parser = argparse.ArgumentParser()
    parser.add_argument('--profile',
        default='default',
        help='the profile to read')
    parser.add_argument('--format',
                        default='powershell',
                        help='the export format')

    known_args = parser.parse_known_args(args)[0]
    config = configparser.ConfigParser()
    filename = os.path.expanduser('~/.aws/credentials')

    config.read(filename)
    access_key_id = config.get(known_args.profile, 'aws_access_key_id')
    secret_access_key = config.get(known_args.profile, 'aws_secret_access_key')
    session_token = config.get(known_args.profile, 'aws_session_token')


    if known_args.format == 'powershell':
        out = '$env:AWS_ACCESS_KEY_ID=\'' + access_key_id + '\''
        out += '; $env:AWS_SECRET_ACCESS_KEY=\'' + secret_access_key + '\''
        out += '; $env:AWS_SESSION_TOKEN=\'' + session_token + '\''
    else:
        raise ValueError('--format parameter [' + known_args.format + '] is not a known value')

    print(out)

    sys.exit(0)

def main():
    sys.exit(cmd(sys.argv))
