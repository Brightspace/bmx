import context
import okta
import os
import pickle
import requests
import unittest
import tempfile
from unittest.mock import Mock
from unittest.mock import patch

import bmx.oktautil

SESSION_ID = 'session id'
APP_URL = 'app url'
SAML_VALUE = 'saml value'

USERNAME = 'john'
PASSWORD = 'cats'
STATE = 'this is the state'
FACTOR_ID = 'factor id'
TOTP_CODE = 'totp code'


class MockCookie():
    cookies = {}
    def __init__(self, cookies={}):
        self.cookies = cookies
    def get_dict(self):
        return self.cookies

class MockSession():
    def __init__(self):
        pass
    def validate_session(self, x):
        return x

class OktaUtilTests(unittest.TestCase):
    @patch('getpass.getpass', return_value=PASSWORD)
    @patch('builtins.input', return_value=TOTP_CODE)
    @patch('requests.get')
    @patch('bmx.oktautil.create_sessions_client')
    @patch('bmx.oktautil.create_auth_client')
    def test_authenticate_should_follow_full_mfa_flow(self, mock_auth_client, *args):
        class PretendClassDict(dict):
            __getattr__ = dict.get

        mock_auth_client.return_value.authenticate.return_value = PretendClassDict({
            'stateToken': STATE,
            'status': 'MFA_REQUIRED',
            'embedded': PretendClassDict({
                'factors': [
                    PretendClassDict({
                        'id': FACTOR_ID,
                        'factorType': 'token:software:totp'
                    })
                ]
            })
        })

        bmx.oktautil.get_new_session(USERNAME)

        mock_auth_client.return_value.authenticate.assert_called_once_with(
            USERNAME,
            PASSWORD
        )
        mock_auth_client.return_value.auth_with_factor.assert_called_once_with(
            STATE,
            FACTOR_ID,
            TOTP_CODE
        )

    def test_create_users_client_should_pass_session_id_always(self):
        okta.UsersClient.__init__ = Mock(return_value=None)

        bmx.oktautil.create_users_client(MockCookie({'sid': SESSION_ID}))

        okta.UsersClient.__init__.assert_called_once_with(
            bmx.oktautil.BASE_URL,
            bmx.oktautil.API_TOKEN,
            headers={
                'Authorization': None,
                'Cookie': 'sid={0}'.format(SESSION_ID)
            }
        )

    @patch('requests.Response')
    def test_connect_to_app_should_return_saml_response_when_one_exists(self, mock_response):
        requests.get = Mock(return_value=mock_response)

        mock_response.raise_for_status.return_value = None
        mock_response.content = """
            <html>
                <head/>
                <body>
                    <input name="SAMLResponse" value="{}"/>
                </body>
            </html>""".format(SAML_VALUE)

        expected_cookies = MockCookie({'sid': SESSION_ID})
        self.assertEqual(
            SAML_VALUE,
            bmx.oktautil.connect_to_app(APP_URL, expected_cookies)
        )

        requests.get.assert_called_once_with(
            APP_URL,
            cookies=expected_cookies
        )

        mock_response.raise_for_status.assert_called_with()

    @patch('os.path.expanduser')
    def test_cached_session_serializes(self, mock_user):
        expected_cached_object = 'expected_cached_object'
        temp_dir = tempfile.mkdtemp()
        mock_user.return_value = temp_dir

        bmx.oktautil.set_cached_session(expected_cached_object)
        with open(os.path.join(temp_dir, 'cookies.state'), 'rb') as test_cookie_state:
            cached_object = pickle.load(test_cookie_state)
            self.assertEqual(expected_cached_object, cached_object)

    @patch('os.path.expanduser')
    @patch('bmx.oktautil.create_sessions_client', return_value=MockSession())
    @patch('pickle.load', return_value=MockCookie({'sid': 'expectedSession'}))
    def test_get_cache_session_exists(self, mock_pickle, mock_session_client, mock_user):
        temp_file = tempfile.mkstemp()[1]
        mock_user.return_value = temp_file

        session, cookies = bmx.oktautil.get_cached_session()
        self.assertTrue(mock_pickle.called)
        self.assertTrue(mock_session_client.called)
        self.assertEqual('expectedSession', session)
        self.assertEqual(cookies.cookies, {'sid': 'expectedSession'})

    def test_cookies_to_string_when_none(self):
        cookie_string = bmx.oktautil.cookie_string(None)
        self.assertEqual('', cookie_string)

    def test_cookies_to_string_when_present(self):
        cookie_string = bmx.oktautil.cookie_string(
            MockCookie({'first': 'first', 'second': 'second'}))
        cookie_parts = cookie_string.split(';')
        self.assertEqual(2, len(cookie_parts))
        self.assertIn('first=first', cookie_parts)
        self.assertIn('second=second', cookie_parts)

if __name__ == '__main__':
    unittest.main()
