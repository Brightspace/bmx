#!/usr/bin/python3

import sys
import json
import argparse

from . import bmxwrite

def create_parser():
    parser = argparse.ArgumentParser()
    parser.add_argument('--username',
        help='specify username instead of being prompted')
    parser.add_argument('--duration', default=3600,
            help='Expiry duration in seconds')

    return parser

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]
    credentials = bmxwrite.get_credentials(known_args.username, known_args.duration)

    out = json.dumps({
        'AccessKeyId': credentials['AccessKeyId'],
        'SecretAccessKey': credentials['SecretAccessKey'],
        'SessionToken': credentials['SessionToken']
    })

    print(out)

    return 0

def main():
    sys.exit(cmd(sys.argv))
