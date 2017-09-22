#!/usr/bin/python3

import base64
import re

import boto3
import lxml

import bmx.oktautil as oktautil
import bmx.prompt as prompt
from bmx.aws_credentials import AwsCredentials

def get_credentials(username, duration_seconds, app=None, role=None):
    session, cookies = oktautil.get_okta_session(username)

    applink = get_app_selection(
        filter_applinks(oktautil.create_users_client(cookies)
                        .get_user_applinks(session.userId)),
        app
    )

    saml_assertion = oktautil.connect_to_app(
        applink.linkUrl,
        cookies
    )

    role = get_role_selection(
        applink.label,
        get_app_roles(base64.b64decode(saml_assertion)),
        role
    )

    first_role, second_role = role.split(',')
    credentials = sts_assume_role(
        saml_assertion,
        first_role,
        second_role,
        duration_seconds=duration_seconds
    )

    return AwsCredentials(credentials, applink.label, second_role)

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
        ).get_selection(force_prompt=app)
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
        ).get_selection(force_prompt=role)
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
