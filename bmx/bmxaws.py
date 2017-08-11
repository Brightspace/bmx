#!/usr/bin/python3

import contextlib
import io
import re
import sys

import awscli.clidriver

import bmx.bmxrenew
import bmx.prompt

def main():
    while True:
        try:
            out = io.StringIO()
            err = io.StringIO()
            with contextlib.redirect_stdout(out):
                with contextlib.redirect_stderr(err):
                    ret = awscli.clidriver.main()
        except SystemExit as e:
            ret = e.code

        if ret == 255 and (
            re.search('ExpiredToken', err.getvalue()) or
            re.search('credentials', err.getvalue())
        ):
            print("Your AWS STS token has expired.  Renewing...")

            bmx.bmxrenew.renew_credentials()
        else:
            break

    errstring = err.getvalue()
    if not bmx.prompt.is_empty(errstring):
        print(errstring, file=sys.stderr)

    outstring = out.getvalue()
    if not bmx.prompt.is_empty(outstring):
        print(outstring)

    return ret

if __name__ == "__main__":
    sys.exit(main())

