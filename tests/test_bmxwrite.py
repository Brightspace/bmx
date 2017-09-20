import os
import unittest
from unittest.mock import MagicMock

from unittest.mock import patch

import bmx.bmxwrite as bmxwrite
from bmx.aws_credentials import AwsCredentials

class MockSession():
    def __init__(self):
        pass
    def userId(self):
        return ''
    def validate_session(self, x):
        return x

def get_mocked_ConfigParser():
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

        mock_config_parser = get_mocked_ConfigParser()
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


if __name__ == '__main__':
    unittest.main()
