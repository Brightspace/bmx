#!/usr/bin/python3

import sys
import argparse

import bmx.bmxaws as bmxaws
import bmx.bmxwrite as bmxwrite
import bmx.bmxprint as bmxprint

class Parser:
    def __init__(self):

        parser = argparse.ArgumentParser(
            add_help=False,
            description='Okta time-out helper for AWS CLI',
            epilog='Copyright 2017 D2L Corporation')

        subparsers = parser.add_subparsers(title='commands')
        aws_parser = subparsers.add_parser('aws',
            help='delegate to the AWS CLI, with automatic STS token renewal',
            add_help=False)
        aws_parser.set_defaults(func=bmxaws.cmd)

        write_parser = subparsers.add_parser('write',
            help='create new AWS credentials and write them to ~/.aws/credentials',
            add_help=False)
        write_parser.set_defaults(func=bmxwrite.cmd)

        print_parser = subparsers.add_parser('print',
            help='create new AWS credentials and print them to stdout',
            add_help=False)
        print_parser.set_defaults(func=bmxprint.cmd)

        self._parser = parser

    def parse_args(self, args):
        return self._parser.parse_known_args(args)

    def usage(self):
        return self._parser.format_help()

def main():
    ret = 1

    try:
        argv = sys.argv
        parser = Parser()

        if len(argv) == 1:
            ret = 2
            print(parser.usage())
        else:
            [known_args, unknown_args] = parser.parse_args(argv[1:])

            if 'func' in known_args:
                ret = known_args.func(unknown_args)
            else:
                ret = 2
                print(parser.usage())
    except Exception as exception:
        print(exception)
        print()
        print(parser.usage())

    return ret

if __name__ == "__main__":
    sys.exit(main())
