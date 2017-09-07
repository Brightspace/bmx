import context
import okta
import requests
import unittest
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

class OktaUtilTests(unittest.TestCase):
    @patch('getpass.getpass', return_value=PASSWORD)
    @patch('builtins.input', return_value=TOTP_CODE)
    def test_authenticate_should_follow_full_mfa_flow(self, *args):
        class PretendClassDict(dict):
            __getattr__ = dict.get

        mock_auth_client = Mock()

        mock_auth_client.authenticate.return_value = PretendClassDict({
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

        bmx.oktautil.authenticate(mock_auth_client, USERNAME)

        mock_auth_client.authenticate.assert_called_once_with(
            USERNAME,
            PASSWORD
        )
        mock_auth_client.auth_with_factor.assert_called_once_with(
            STATE,
            FACTOR_ID,
            TOTP_CODE
        )

    def test_create_users_client_should_pass_session_id_always(self):
        okta.UsersClient.__init__ = Mock(return_value=None)

        bmx.oktautil.create_users_client(SESSION_ID)

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

        self.assertEqual(
            SAML_VALUE,
            bmx.oktautil.connect_to_app(APP_URL, SESSION_ID)
        )

        requests.get.assert_called_once_with(
            APP_URL,
            headers={'Cookie': 'sid={0}'.format(SESSION_ID)}
        )

        mock_response.raise_for_status.assert_called_with()

if __name__ == '__main__':
    unittest.main();
