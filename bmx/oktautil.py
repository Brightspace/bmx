#!/usr/bin/python3

import getpass
import os
import pickle

import okta
from okta.framework.OktaError import OktaError
from lxml import html
import requests

from . import prompt

BASE_URL = 'https://d2l.okta.com/'
API_TOKEN = 'apitoken'

# TODO: Create directories and files with correct permissions, central method will be available
def set_cached_session(cookies):
    local_settings_dir = os.path.join(os.path.expanduser('~'), '.bmx')
    if not os.path.exists(local_settings_dir):
        os.makedirs(local_settings_dir)
    with open(os.path.join(local_settings_dir, 'cookies.state'), 'wb') as cookie_state:
        pickle.dump(cookies, cookie_state)

# TODO: Use common get_dir method that will be available
def get_cached_session():
    try:
        local_settings_dir = os.path.join(os.path.expanduser('~'), '.bmx')
        with open(os.path.join(local_settings_dir, 'cookies.state'), 'rb') as cookie_state:
            cookies = pickle.load(cookie_state)
        sessions_client = create_sessions_client(cookies)
        session = sessions_client.validate_session(cookies.get_dict()['sid'])
    except Exception:
        session = cookies = None
    return session, cookies

def get_new_session(username):
    auth_client = create_auth_client()
    sessions_client = create_sessions_client()

    while True:
        try:
            username = username or prompt.prompt_for_value(input, 'Okta username: ')
            password = prompt.prompt_for_value(getpass.getpass, 'Okta password: ')
            authentication = auth_client.authenticate(username, password)
            if authentication.status == 'MFA_REQUIRED':
                state = authentication.stateToken
                factors = authentication.embedded.factors

                # This could likely support SMS, but haven't tested that as I don't use it

                totp_factors = [f for f in factors if f.factorType == 'token:software:totp']
                if totp_factors:
                    factor_id = totp_factors[0].id
                    totp_code = prompt.prompt_for_value(input, 'Okta TOTP code: ')
                    authentication = auth_client.auth_with_factor(state, factor_id, totp_code)
                else:
                    factor_types = [f.factorType for f in factors]
                    message = (
                        'MFA required by Okta, but no supported factors available.'
                        '\n   Supported: [\'token:software:totp\']'
                        '\n   Available: {0}'
                    ).format(factor_types)
                    raise NotImplementedError(message)

            session = sessions_client.create_session_by_session_token(
                authentication.sessionToken, additional_fields="cookieToken,cookieTokenUrl")
            cookies = requests.get(session.cookieTokenUrl).cookies

            return session, cookies
        except (OktaError, requests.HTTPError) as ex:
            print(ex)

def get_okta_session(username):
    session, cookies = get_cached_session()
    if None in (session, cookies):
        session, cookies = get_new_session(username)
        set_cached_session(cookies)
    return session, cookies

def cookie_string(cookies):
    cookie_header = ''
    if cookies:
        cookie_header = ';'.join(
            ['%s=%s' % (key, value) for (key, value) in cookies.get_dict().items()])
    return cookie_header

def create_auth_client():
    return okta.AuthClient(
        BASE_URL,
        API_TOKEN,
        headers={'Authorization': None}
    )

def create_sessions_client(cookies=None):
    return okta.SessionsClient(
        BASE_URL,
        API_TOKEN,
        headers={
            'Authorization': None,
            'Cookie': cookie_string(cookies)
        }
    )

def create_users_client(cookies):
    return okta.UsersClient(
        BASE_URL,
        API_TOKEN,
        headers={
            'Authorization': None,
            'Cookie': cookie_string(cookies)
        }
    )

def connect_to_app(app_url, cookies):
    response = requests.get(
        app_url,
        cookies=cookies
    )
    response.raise_for_status()

    return html.fromstring(
        response.content
    ).xpath("//input[@name='SAMLResponse']/@value")[0]
