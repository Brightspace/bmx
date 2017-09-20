import argparse

import bmx.credentialsutil as credentialsutil
import bmx.stsutil as stsutil

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx renew',
        usage='''

bmx renew -h
bmx renew [--username USERNAME]
          [--duration DURATION]
          [--account ACCOUNT]
          [--role ROLE]'''
)
    parser.add_argument('--username',
        help='specify username instead of being prompted')

    parser.add_argument(
        '--duration',
        default=3600,
        help='the requested STS-token lease duration'
    )

    parser.add_argument('--account', default=None, help='the aws account name to auth against')

    parser.add_argument('--role', default=None, help='the aws role name to auth as')

    return parser

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    credentialsutil.write_credentials(stsutil.get_credentials(
            known_args.username, known_args.duration,
            app=known_args.account, role=known_args.role))

    return 0
