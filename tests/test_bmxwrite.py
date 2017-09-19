import base64
import contextlib
import io
import json
import os
import unittest

from unittest.mock import Mock, MagicMock
from unittest.mock import patch

import bmx.bmxprint as bmxprint
import bmx.bmxwrite as bmxwrite
import okta.models.user
import bmx.prompt as prompt
import bmx.credentialsutil as credentialsutil
from bmx.aws_credentials import AwsCredentials

class MockSession():
    def __init__(self):
        pass
    def userId(self):
        return ''
    def validate_session(self, x):
        return x

class BmxWriteTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmxwrite.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(4, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--profile', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--account', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

        self.assertEqual('--role', calls[3][0][0])
        self.assertTrue('help' in calls[3][1])


    #@patch('builtins.open')
    #@patch('boto3.client')
    #@patch('configparser.ConfigParser', return_value=MagicMock())
    #@patch('bmx.stsutil.get_app_roles')
    #@patch('bmx.stsutil.oktautil')
    #@patch('bmx.prompt.prompt_for_value', return_value="password")
    #def test_write_with_account_and_role_should_write_correct_data(self,
                                                                   #mock_prompt,
                                                                   #mock_oktautil,
                                                                   #mock_get_app_roles,
                                                                   #mock_configparser,
                                                                   #mock_boto3,
                                                                   #mock_open):
        #def getAppLink(props):
            #applink = okta.models.user.AppLinks()
            #for key, value in props.items():
                #applink.__setattr__(key, value)
            #return applink


        #mock_oktautil.get_okta_session.return_value = MockSession(), 'someCookieObject'

        #mock_oktautil.create_users_client.return_value.get_user_applinks.return_value = [
            #getAppLink({'appName': 'amazon_aws', 'label': 'not-my-account'}),
            #getAppLink({'appName': 'amazon_aws', 'label': 'my-account'})
        #]
#
        #mock_oktautil.connect_to_app.return_value = base64.b64encode(b'skip_me')

        #mock_get_app_roles.return_value = [
            #'arn:aws:iam::accountid:saml-provider/Okta,arn:aws:iam::accountid:role/my-role',
            #'arn:aws:iam::accountid:saml-provider/Okta,arn:aws:iam::accountid:role/not-my-role'
        #]

        #mock_boto3.return_value.assume_role_with_saml.return_value = {'Credentials':
                                                                          #{'AccessKeyId': 'my-access-key',
                                                                           #'SecretAccessKey': 'my-secret-access-key',
                                                                           #'SessionToken': 'my-session-token'}
                                                                      #}

        #bmxwrite.cmd(['--profile', 'my-profile',
                      #'--account', 'my-account',
                      #'--username', 'my-user',
                      #'--role', 'my-role'])

        #mock_configparser.return_value.__setitem__.assert_called_with('my-profile', {'aws_access_key_id': 'my-access-key',
                                                                                    #'aws_secret_access_key': 'my-secret-access-key',
                                                                                    #'aws_session_token': 'my-session-token'})
        #assert mock_configparser.return_value.write.called
        #mock_open.assert_called_with(os.path.expanduser('~/.aws/credentials'), 'w')

    #@patch('bmx.bmxprint.create_parser')
    #def test_cmd_should_print_credentials_always(self, mock_arg_parser):
        #return_value = {
            #'AccessKeyId': ACCESS_KEY_ID,
            #'SecretAccessKey': SECRET_ACCESS_KEY,
            #'SessionToken': SESSION_TOKEN
        #}

        #bmx.bmxwrite.get_credentials = Mock(
            #return_value=return_value
        #)

        #out = io.StringIO()
        #with contextlib.redirect_stdout(out):
            #self.assertEqual(0, bmx.bmxprint.cmd([]))
        #out.seek(0)
        #printed = json.load(out)

        #self.assertEqual(return_value, printed)

if __name__ == '__main__':
    unittest.main();
