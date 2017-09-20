import argparse

import bmx.credentialsutil as credentialsutil
import bmx.stsutil as stsutil
from bmx.locale.options import (BMX_RENEW_USAGE,
                                BMX_ACCOUNT_HELP, BMX_DURATION_HELP,
                                BMX_ROLE_HELP, BMX_USERNAME_HELP)

def create_parser():
    parser = argparse.ArgumentParser(
        prog='bmx renew',
        usage=BMX_RENEW_USAGE
    )
    parser.add_argument('--username',
        help=BMX_USERNAME_HELP)

    parser.add_argument(
        '--duration',
        default=3600,
        help=BMX_DURATION_HELP
    )

    parser.add_argument('--account', default=None, help=BMX_ACCOUNT_HELP)

    parser.add_argument('--role', default=None, help=BMX_ROLE_HELP)

    return parser

def cmd(args):
    known_args = create_parser().parse_known_args(args)[0]

    credentialsutil.write_credentials(stsutil.get_credentials(
            known_args.username, known_args.duration,
            app=known_args.account, role=known_args.role))

    return 0
