import os
import unittest

from unittest.mock import (MagicMock, Mock, patch)

import bmx.bmxwrite as bmxwrite
from bmx.aws_credentials import AwsCredentials
from bmx.locale.options import (BMX_WRITE_USAGE, BMX_WRITE_PROFILE_HELP,
                                BMX_ACCOUNT_HELP, BMX_ROLE_HELP, BMX_USERNAME_HELP)


class BmxWriteTests(unittest.TestCase):
    def get_mocked_configparser(self):
        config_parser_dict = {}

        def getitem(k):
            return config_parser_dict[k]

        def setitem(k, v):
            config_parser_dict[k] = v

        mock_config_parser = MagicMock()
        mock_config_parser.__getitem__.side_effect = getitem
        mock_config_parser.__setitem__.side_effect = setitem
        mock_config_parser.get_dict.return_value = config_parser_dict

        return mock_config_parser

    @patch('argparse.ArgumentParser', autospec=True)
    def test_create_parser_should_create_expected_parser_always(self, mock_argparser_constructor):
        parser = bmxwrite.create_parser()


        mock_argparser_constructor.assert_called_once_with(prog='bmx write', usage=BMX_WRITE_USAGE)
        parser.add_argument.assert_any_call('--username', default=None, help=BMX_USERNAME_HELP)
        parser.add_argument.assert_any_call('--profile', default='default', help=BMX_WRITE_PROFILE_HELP)
        parser.add_argument.assert_any_call('--account', default=None, help=BMX_ACCOUNT_HELP)
        parser.add_argument.assert_any_call('--role', default=None, help=BMX_ROLE_HELP)

    def test_get_aws_path(self):
        with patch('os.path.expanduser', return_value='user_home'):
            expected_aws_path = os.path.join('user_home', '.aws')
            actual_aws_path = bmxwrite.get_aws_path()
            self.assertEqual(expected_aws_path, actual_aws_path)

    def test_get_credentials_path(self):
        with patch('bmx.bmxwrite.get_aws_path', return_value='aws_home'):
            expected_aws_path = os.path.join('aws_home', 'credentials')
            actual_aws_path = bmxwrite.get_credentials_path()
            self.assertEqual(expected_aws_path, actual_aws_path)

    @patch('bmx.bmxwrite.get_credentials_path', return_value='credential_path')
    @patch('configparser.ConfigParser')
    def test_write_credentials(self, mock_config, *args):
        credentials = AwsCredentials({
            'AccessKeyId': 'expectedAccessKeyId',
            'SecretAccessKey': 'expectedSecretAccessKey',
            'SessionToken': 'expectedSessionToken'
        }, 'expected_account', 'expected_role')

        mock_config_parser = self.get_mocked_configparser()
        mock_config.return_value = mock_config_parser

        bmxwrite.write_credentials(credentials, 'expected_profile')

        mock_config_parser.read.assert_called_once()
        self.assertDictEqual({
            'expected_profile': {
                'aws_access_key_id': 'expectedAccessKeyId',
                'aws_secret_access_key': 'expectedSecretAccessKey',
                'aws_session_token': 'expectedSessionToken'
            }
        }, mock_config_parser.get_dict())
        mock_config_parser.write.assert_called_once()

    @patch('bmx.stsutil.get_credentials')
    @patch('bmx.credentialsutil.load_bmx_credentials')
    @patch('bmx.bmxwrite.write_credentials')
    @patch('bmx.bmxwrite.create_parser')
    def test_bmxremove_calls_required_methods(self,
                                              mock_create_parser,
                                              mock_write_credentials,
                                              mock_load_bmx_credentials,
                                              mock_get_credentials):
        expected_args = 'expected_args'
        expected_username = 'expected_username'
        expected_account = 'expected_account'
        expected_role = 'expected_role'
        expected_profile = 'expected_profile'
        expected_aws_credentials = 'expected_aws_credentials'
        mock_create_parser.return_value.parse_known_args.return_value = [
            Mock(
            ** {
                'username': expected_username,
                'account': expected_account,
                'role': expected_role,
                'profile': expected_profile
            }
        )]

        mock_bmx_credentials = mock_load_bmx_credentials.return_value
        mock_bmx_credentials.get_credentials.return_value = None
        mock_get_credentials.return_value = expected_aws_credentials
        mock_load_bmx_credentials.return_value = mock_bmx_credentials

        self.assertEqual(0, bmxwrite.cmd(expected_args))

        mock_create_parser.return_value.parse_known_args.assert_called_once_with(expected_args)
        mock_load_bmx_credentials.assert_called_once()
        mock_bmx_credentials.get_credentials.assert_called_once_with(expected_account, expected_role)
        mock_get_credentials.assert_called_once_with(expected_username, 3600, expected_account, expected_role)
        mock_write_credentials.assert_called_once_with(expected_aws_credentials, expected_profile)
        mock_bmx_credentials.put_credentials.assert_called_once_with(expected_aws_credentials)
        mock_bmx_credentials.write.assert_called_once()


if __name__ == '__main__':
    unittest.main()
