#!/usr/bin/python3

import base64
import configparser
import getpass
import sys
import os
import re
import argparse

import boto3
import lxml

from okta.framework.OktaError import OktaError
from requests import HTTPError

from . import prompt
from . import oktautil

def renew_credentials(username=None, duration_seconds=3600):
    write_credentials(get_credentials(username, duration_seconds))

def get_credentials(username, duration_seconds):
    auth_client = oktautil.create_auth_client()
    sessions_client = oktautil.create_sessions_client()

    while True:
        try:
            username = username or prompt.prompt_for_value(input, 'Okta username: ')
            password = prompt.prompt_for_value(getpass.getpass, 'Okta password: ')

            authentication = auth_client.authenticate(username, password)
            session = sessions_client.create_session(username, password)

            applink = get_app_selection(
                filter_applinks(
                    oktautil.create_users_client(session.id).
                        get_user_applinks(session.userId)
                )
            )

            saml_assertion = oktautil.connect_to_app(
                applink.linkUrl,
                authentication.sessionToken
            )

            role = get_role_selection(
                applink.label,
                get_app_roles(base64.b64decode(saml_assertion))
            )
            split_role = role.split(',')

            credentials = sts_assume_role(
                saml_assertion,
                split_role[0],
                split_role[1],
                duration_seconds=duration_seconds
            )

            break
        except OktaError as ex:
            print(ex)
        except HTTPError as ex:
            print(ex)

    return credentials

def filter_applinks(applinks):
    return sorted(
        filter(
            lambda x: x.appName == "amazon_aws",
            applinks
        ),
        key=lambda x: x.label
    )

def get_app_selection(applinks):
    return applinks[
        prompt.MinMenu(
            '\nAvailable AWS Accounts: ',
            list(map(lambda x: '{}'.format(x.label), applinks)),
            'AWS Account Index: '
        ).get_selection()
    ]

def get_role_selection(app_name, roles):
    return roles[
        prompt.MinMenu(
            '\nAvailable Roles in {}:'.format(app_name),
            list(map(lambda x: re.sub('.*role/', '', x.split(',')[1]), roles)),
            'Role Index: '
        ).get_selection()
    ]

def get_app_roles(saml_assertion):
    return lxml.etree.fromstring(saml_assertion).xpath(
        ''.join([
            '//x:AttributeStatement',
            '/x:Attribute',
            '[@Name="https://aws.amazon.com/SAML/Attributes/Role"]/x:AttributeValue/text()'
        ]),
        namespaces={'x': 'urn:oasis:names:tc:SAML:2.0:assertion'}
    )

def sts_assume_role(saml_assertion, principal, role, duration_seconds):
    response = boto3.client('sts').assume_role_with_saml(
        PrincipalArn=principal,
        RoleArn=role,
        SAMLAssertion=saml_assertion,
        DurationSeconds=duration_seconds
    )

    return response['Credentials']

def write_credentials(credentials):
    config = configparser.ConfigParser()
    filename = os.path.expanduser('~/.aws/credentials')

    config.read(filename)
    config['default'] = {
        'aws_access_key_id': credentials['AccessKeyId'],
        'aws_secret_access_key': credentials['SecretAccessKey'],
        'aws_session_token': credentials['SessionToken']
    }

    with open(os.path.expanduser('~/.aws/credentials'), 'w') as config_file:
        config.write(config_file)

def cmd(args):
    parser = argparse.ArgumentParser()
    parser.add_argument('--username',
        help='specify username instead of being prompted')

    [known_args, unknown_args] = parser.parse_known_args(args)
    renew_credentials(known_args.username)

    return 0

def main():
    sys.exit(cmd(sys.argv))
