import sys
import argparse

import bmx.credentialsutil as credentialsutil

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx remove',
        usage='''
bmx remove -h
bmx remove [--account ACCOUNT] [--role ROLE] CLICOMMAND
'''
    )
    parser.add_argument('--account', default=None, help='the aws account name to auth against')
    parser.add_argument('--role', default=None, help='the aws role name to auth as')


    return parser

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]
    credentialsutil.remove_credentials(known_args.account, known_args.role)

    return 0

def main():
    sys.exit(cmd(sys.argv))
