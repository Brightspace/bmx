#!/usr/bin/python3

import sys
import json
from . import bmxrenew

def main(args):
    credentials = bmxrenew.get_credentials(args.username, args.duration)

    out = json.dumps({
        'AccessKeyId': credentials['AccessKeyId'],
        'SecretAccessKey': credentials['SecretAccessKey'],
        'SessionToken': credentials['SessionToken']
    })

    print (out)

    sys.exit(0)
