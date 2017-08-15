#!/usr/bin/python3

import sys
import json
import argparse

from . import bmxwrite

def cmd(args):
    parser = argparse.ArgumentParser()
    parser.add_argument('--username',
        help='specify username instead of being prompted')
    parser.add_argument('--duration', required=False, default=3600,
            help='Expiry duration in seconds')

    [known_args, unknown_args] = parser.parse_known_args(args)
    credentials = bmxwrite.get_credentials(known_args.username, known_args.duration)

    out = json.dumps({
        'AccessKeyId': credentials['AccessKeyId'],
        'SecretAccessKey': credentials['SecretAccessKey'],
        'SessionToken': credentials['SessionToken']
    })

    print(out)

    sys.exit(0)

def main():
    sys.exit(cmd(sys.argv))
