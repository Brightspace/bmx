#!/usr/bin/python3

import getpass
import okta
import requests

from lxml import html
from . import prompt

BASE_URL = 'https://d2l.okta.com/'
API_TOKEN = 'apitoken'

def create_auth_client():
    return okta.AuthClient(
        BASE_URL,
        API_TOKEN,
        headers={'Authorization': None}
    )

def authenticate(auth_client, username=None):
    username = username or prompt.prompt_for_value(input, 'Okta username: ')
    password = prompt.prompt_for_value(getpass.getpass, 'Okta password: ')

    authentication = auth_client.authenticate(username, password)

    if authentication.status == 'MFA_REQUIRED':
        state = authentication.stateToken
        factors = authentication.embedded.factors

        # This could likely support SMS, but haven't tested that as I don't use it

        totp_factors = list(filter(lambda f: f.factorType == 'token:software:totp', factors))
        if totp_factors:
            factor_id = totp_factors[0].id
            totp_code = prompt.prompt_for_value(input, 'Okta TOTP code: ')

            authentication = auth_client.auth_with_factor(state, factor_id, totp_code)
        else:
            factor_types = list(map(lambda f: f.factorType, factors))
            message = (
                'MFA required by Okta, but no supported factors available.'
                '\n   Supported: [\'token:software:totp\']'
                '\n   Available: {0}'
            ).format(factor_types)
            raise NotImplementedError(message)

    return authentication

def create_sessions_client():
    return okta.SessionsClient(
        BASE_URL,
        API_TOKEN,
        headers={'Authorization': None}
    )

def create_users_client(session_id):
    return okta.UsersClient(
        BASE_URL,
        API_TOKEN,
        headers={
            'Authorization': None,
            'Cookie': 'sid={0}'.format(session_id)
        }
    )

def connect_to_app(app_url, session_id):
    response = requests.get(
        app_url,
        headers={'Cookie': 'sid={0}'.format(session_id)}
    )
    response.raise_for_status()

    return html.fromstring(
        response.content
    ).xpath("//input[@name='SAMLResponse']/@value")[0]
