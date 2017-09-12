#!/usr/bin/python3

import base64
import boto3
import lxml
import re

from okta.framework.OktaError import OktaError
from requests import HTTPError

import bmx.oktautil as oktautil
import bmx.prompt as prompt

def get_credentials(username, duration_seconds, app=None, role=None):
    auth_client = oktautil.create_auth_client()
    sessions_client = oktautil.create_sessions_client()

    while True:
        try:
            authentication = oktautil.authenticate(auth_client, username)
            session = sessions_client.create_session_by_session_token(authentication.sessionToken)


            applink = get_app_selection(
                filter_applinks(oktautil.create_users_client(session.id)
                                .get_user_applinks(session.userId)),
                app
            )

            saml_assertion = oktautil.connect_to_app(
                applink.linkUrl,
                session.id
            )

            role = get_role_selection(
                applink.label,
                get_app_roles(base64.b64decode(saml_assertion)),
                role
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

def get_app_selection(applinks, app=None):
    if app:
        found_app = next((x for x in applinks if x.label.lower() == app.lower()), None)
        if found_app:
            return found_app

    return applinks[
        prompt.MinMenu(
            '\nAvailable AWS Accounts: ',
            list(map(lambda x: '{}'.format(x.label), applinks)),
            'AWS Account Index: '
        ).get_selection()
    ]

def get_role_selection(app_name, roles, role=None):
    if role:
        found_role = next((x for x in roles if re.sub('.*role/', '',
                            x.split(',')[1]).lower() == role.lower()), None)
        if found_role:
            return found_role

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
