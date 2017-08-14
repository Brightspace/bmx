#!/usr/bin/python3

import sys
import argparse

from . import bmxaws
from . import bmxrenew
from . import bmxprint

class Parser:
    def __init__(self):
        def add_username_arg(sub_parser):
            sub_parser.add_argument('--username', required=False,
                help='Use the environment variable USERNAME as the username')

        parser = argparse.ArgumentParser(
            description='Okta time-out helper for AWS CLI',
            epilog='Copyright 2017 D2L Corporation')
        subparsers = parser.add_subparsers(title='okta',
            description='Automatic STS token renewal from Okta',
            help='')
        aws_parser = subparsers.add_parser('aws', help='awscli with automatic STS token renewal')
        aws_parser.set_defaults(func=self.aws)
        add_username_arg(aws_parser)

        renew_parser = subparsers.add_parser('renew', help='renew default credentials')
        renew_parser.set_defaults(func=self.renew)
        add_username_arg(renew_parser)

        print_parser = subparsers.add_parser('print', help='renew default credentials and print to console')
        print_parser.set_defaults(func=self.print)
        print_parser.add_argument('--duration', required=False, default=3600,
            help='Expiry duration in seconds')
        add_username_arg(print_parser)

        self._parser = parser

    def aws(self, known_args, unknown_args):
        return bmxaws.main(unknown_args, known_args.username)

    def renew(self, known_args, unknown_args):
        return bmxrenew.main(known_args)

    def print(self, known_args, unknown_args):
        return bmxprint.main(known_args)

    def parse_args(self, args):
        return self._parser.parse_known_args(args)

    def usage(self):
        return self._parser.format_help()

def main():
    argv = sys.argv
    parser = Parser()
    if len(argv) == 1:
        print(parser.usage())
        return 1

    [known_args, unknown_args] = parser.parse_args(argv[1:])
    return known_args.func(known_args, unknown_args)

if __name__ == "__main__":
    sys.exit(main())

