import sys
import argparse

import bmx.credentialsutil as credentialsutil
from bmx.locale.options import (BMX_REMOVE_USAGE,
                                BMX_ACCOUNT_HELP, BMX_ROLE_HELP)

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx remove',
        usage=BMX_REMOVE_USAGE
    )
    parser.add_argument('--account', default=None, help=BMX_ACCOUNT_HELP)
    parser.add_argument('--role', default=None, help=BMX_ROLE_HELP)

    return parser

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    bmx_credentials = credentialsutil.load_bmx_credentials()
    bmx_credentials.remove_credentials(known_args.account, known_args.role)
    bmx_credentials.write()

    return 0
