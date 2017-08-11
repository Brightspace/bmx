#!/usr/bin/python3

import sys
import cmd

import bmx.bmxaws
import bmx.bmxrenew

class Bmx(cmd.Cmd):
    def do_aws(self, myarg):
        sys.argv.insert(0, 'bmxaws')

        return bmx.bmxaws.main()

    def do_writecreds(self, myarg):
        return bmx.bmxrenewcreds.main()

def main():
    sys.argv.pop(0)
    command = sys.argv.pop(0)

    return Bmx().onecmd(command)

if __name__ == "__main__":
    sys.exit(main())

