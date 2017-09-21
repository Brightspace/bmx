import argparse
import unittest

from unittest.mock import patch, Mock

import bmx.bmxrenew as bmxrenew

USERNAME='username'
APP='app'
ROLE='role'

def create_mock_known_args(app=None, role=None):
    return Mock(
            **{
                'username': USERNAME,
                'account': app,
                'role': role
            })

def create_mock_aws_credentials():
    return Mock(
            **{
                'keys': {
                    'AccessKeyId': 'access key id',
                    'SecretAccessKey': 'secret access key',
                    'SessionToken': 'session token'
                }
            })

class BmxRenewTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmxrenew.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(3, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--account', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--role', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

    @patch('bmx.stsutil.get_credentials', return_value=create_mock_aws_credentials())
    @patch('bmx.credentialsutil.load_bmx_credentials')
    @patch('bmx.bmxrenew.create_parser')
    def test_cmd_should_delegate_to_aws_always(self,
            mock_create_parser, mock_load_bmx_credentials, mock_sts_get_credentials):

        mock_parser = mock_create_parser.return_value
        mock_parser.parse_known_args.return_value = [create_mock_known_args(),None]

        mock_bmx_credentials = mock_load_bmx_credentials.return_value
        mock_bmx_credentials.get_default_reference.return_value = (APP, ROLE)

        self.assertEqual(0, bmxrenew.cmd([]))

        mock_parser.parse_known_args.assert_called_with([])
        mock_bmx_credentials.get_default_reference.assert_called()
        mock_sts_get_credentials.assert_called_with(USERNAME, 3600, APP, ROLE)
        mock_bmx_credentials.put_credentials.assert_called_with(
                mock_sts_get_credentials.return_value)

if __name__ == '__main__':
    unittest.main()
