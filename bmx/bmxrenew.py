import argparse

import bmx.credentialsutil as credentialsutil
import bmx.stsutil as stsutil
from bmx.options import (BMX_RENEW_USAGE, BMX_ACCOUNT_HELP,
                     BMX_ROLE_HELP, BMX_USERNAME_HELP)


def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx renew',
        usage=BMX_RENEW_USAGE
    )
    parser.add_argument('--username', help=BMX_USERNAME_HELP)

    parser.add_argument('--account', default=None, help=BMX_ACCOUNT_HELP)

    parser.add_argument('--role', default=None, help=BMX_ROLE_HELP)

    return parser

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    bmx_credentials = credentialsutil.load_bmx_credentials()

    if not known_args.account and not known_args.role:
        app, role = bmx_credentials.get_default_reference()
    else:
        app, role = known_args.account, known_args.role

    bmx_credentials.put_credentials(stsutil.get_credentials(
                known_args.username, 3600, app, role))

    bmx_credentials.write()

    return 0
