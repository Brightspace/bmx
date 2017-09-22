import argparse
import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

import awscli.clidriver

from .context import bmx
import bmx.bmxaws
import bmx.credentialsutil

def create_mock_aws_credentials():
    return Mock(
            **{
                'keys': {
                    'AccessKeyId': 'access key id',
                    'SecretAccessKey': 'secret access key',
                    'SessionToken': 'session token'
                }
            })

class BmxAwsTests(unittest.TestCase):

    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser_constructor):
        parser = bmx.bmxaws.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(3, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--account', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--role', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

    @patch('awscli.clidriver.create_clidriver')
    @patch('bmx.credentialsutil.load_bmx_credentials')
    @patch('bmx.bmxaws.create_parser')
    def test_cmd_should_delegate_to_aws_always(self,
            mock_create_parser, mock_load_bmx_credentials, mock_create_clidriver):

        known_args = Mock(
            **{
                'username': 'username',
                'account': 'account',
                'role': 'role'
            }
        )
        unknown_args = 'unknown args'
        args = ['arg']

        mock_parser = mock_create_parser.return_value
        mock_parser.parse_known_args.return_value = [known_args,unknown_args]

        mock_aws_credentials = create_mock_aws_credentials()
        mock_bmx_credentials = mock_load_bmx_credentials.return_value
        mock_bmx_credentials.get_credentials.return_value = mock_aws_credentials

        mock_clidriver = mock_create_clidriver.return_value
        mock_clidriver.main.return_value = 0

        self.assertEqual(0, bmx.bmxaws.cmd(args))

        mock_parser.parse_known_args.assert_called_with(args)
        mock_bmx_credentials.get_credentials.assert_called_with(
                app='account', role='role')
        mock_clidriver.main.assert_called_with(unknown_args)

        mock_bmx_credentials.put_credentials.assert_called_with(mock_aws_credentials)
        mock_bmx_credentials.write.assert_called()

    @patch('awscli.clidriver.create_clidriver')
    @patch('bmx.credentialsutil.load_bmx_credentials')
    def test_cmd_with_account_and_role_should_pass_correct_args_to_awscli(self,
            mock_load_bmx_credentials, mock_create_clidriver):

        known_args = ['--account', 'my-account',
                      '--username', 'my-user',
                      '--role', 'my-role']
        unknown_args = ['aws_command', 'sub_command']
        args = known_args + unknown_args

        mock_aws_credentials = create_mock_aws_credentials()
        mock_bmx_credentials = mock_load_bmx_credentials.return_value
        mock_bmx_credentials.get_credentials.return_value = mock_aws_credentials

        mock_clidriver = mock_create_clidriver.return_value
        mock_clidriver.main.return_value = 0

        bmx.bmxaws.cmd(args)

        mock_clidriver.main.assert_called_with(unknown_args)

        mock_bmx_credentials.put_credentials.assert_called_with(mock_aws_credentials)
        mock_bmx_credentials.write.assert_called()

if __name__ == '__main__':
    unittest.main()
