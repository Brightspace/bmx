#!/usr/bin/python3

import getpass
import pickle

import okta
from okta.framework.OktaError import OktaError
from lxml import html
import requests

from . import prompt
from . import credentialsutil

BASE_URL = 'https://d2l.okta.com/'
API_TOKEN = 'apitoken'
SUPPORTED_FACTORS = ['sms', 'token:software:totp']

def set_cached_session(cookies):
    credentialsutil.create_bmx_path()
    with open(credentialsutil.get_cookie_session_path(), 'wb') as cookie_state:
        pickle.dump(cookies, cookie_state)

def get_cached_session():
    try:
        with open(credentialsutil.get_cookie_session_path(), 'rb') as cookie_state:
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

                available_factors = [f for f in factors if f.factorType in SUPPORTED_FACTORS]
                if available_factors:
                    factor = get_factor_selection(available_factors)
                    if factor.factorType == 'sms':
                        # Send the SMS message by sending an empty passcode
                        auth_client.auth_with_factor(state, factor.id, None)
                    code = prompt.prompt_for_value(input, 'Okta MFA code: ')

                    authentication = auth_client.auth_with_factor(state, factor.id, code)
                else:
                    factor_types = [f.factorType for f in factors]
                    message = (
                        'MFA required by Okta, but no supported factors available.'
                        '\n   Supported: {0}'
                        '\n   Available: {1}'
                    ).format(SUPPORTED_FACTORS, factor_types)
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

def get_factor_selection(factors):
    factor_labels = {
        ('sms', 'OKTA'): 'SMS',
        ('token:software:totp', 'GOOGLE'): 'Google Authenticator Mobile App',
        ('token:software:totp', 'OKTA'): 'Okta Verify Mobile App'
    }
    return factors[
        prompt.MinMenu(
            '\nAvailable Authentication Methods: ',
            [factor_labels.get((f.factorType, f.provider), f.factorType) for f in factors],
            'Index: '
        ).get_selection()
    ]

def connect_to_app(app_url, cookies):
    response = requests.get(
        app_url,
        cookies=cookies
    )
    response.raise_for_status()

    return html.fromstring(
        response.content
    ).xpath("//input[@name='SAMLResponse']/@value")[0]
