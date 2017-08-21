import context
import okta
import requests
import unittest
from unittest.mock import Mock
from unittest.mock import patch

import bmx.oktautil

SESSION_ID = 'session id'
APP_URL = 'app url'
SESSION_TOKEN = 'session token'
SAML_VALUE = 'saml value'

class OktaUtilTests(unittest.TestCase):
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
            bmx.oktautil.connect_to_app(APP_URL, SESSION_TOKEN)
        )

        requests.get.assert_called_once_with(
            APP_URL,
            params={'onetimetoken': SESSION_TOKEN}
        )
        mock_response.raise_for_status.assert_called_once()

if __name__ == '__main__':
    unittest.main();
