import sys
import argparse

import bmx.bmxaws as bmxaws
import bmx.bmxwrite as bmxwrite
import bmx.bmxprint as bmxprint
import bmx.bmxrenew as bmxrenew
import bmx.bmxremove as bmxremove
from bmx.options import (BMX_COPYRIGHT, BMX_DESCRIPTION,
                     BMX_AWS_HELP, BMX_PRINT_HELP, BMX_REMOVE_HELP,
                     BMX_RENEW_HELP, BMX_WRITE_HELP)


class Parser:
    def __init__(self):
        parser = argparse.ArgumentParser(
            add_help=False,
            description=BMX_DESCRIPTION,
            epilog=BMX_COPYRIGHT)

        subparsers = parser.add_subparsers(title='commands')
        aws_parser = subparsers.add_parser('aws',
            help=BMX_AWS_HELP,
            add_help=False)
        aws_parser.set_defaults(func=bmxaws.cmd)

        write_parser = subparsers.add_parser('write',
            help=BMX_WRITE_HELP,
            add_help=False)
        write_parser.set_defaults(func=bmxwrite.cmd)

        print_parser = subparsers.add_parser('print',
            help=BMX_PRINT_HELP,
            add_help=False)
        print_parser.set_defaults(func=bmxprint.cmd)

        renew_parser = subparsers.add_parser('renew',
            help=BMX_RENEW_HELP,
            add_help=False)
        renew_parser.set_defaults(func=bmxrenew.cmd)

        remove_parser = subparsers.add_parser('remove',
            help=BMX_REMOVE_HELP,
            add_help=False)
        remove_parser.set_defaults(func=bmxremove.cmd)

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
