#!/usr/bin/python3

import sys
import argparse

from . import bmxaws
from . import bmxrenew
from . import bmxprint

class Parser:
    def __init__(self):

        parser = argparse.ArgumentParser(
            description='Okta time-out helper for AWS CLI',
            epilog='Copyright 2017 D2L Corporation')
        parser.add_argument('--username', required=False,
            help='specify username instead of being prompted')

        subparsers = parser.add_subparsers(title='commands')
        aws_parser = subparsers.add_parser('aws', help='awscli with automatic STS token renewal')
        aws_parser.set_defaults(func=self.aws)

        write_parser = subparsers.add_parser('write', help='write default credentials')
        write_parser.set_defaults(func=self.write)

        print_parser = subparsers.add_parser('print', help='write default credentials and print to console')
        print_parser.set_defaults(func=self.print)
        print_parser.add_argument('--duration', required=False, default=3600,
            help='Expiry duration in seconds')

        self._parser = parser

    def aws(self, known_args, unknown_args):
        return bmxaws.main(known_args, unknown_args)

    def write(self, known_args, unknown_args):
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

