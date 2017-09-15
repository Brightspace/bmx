import argparse
import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

import awscli.clidriver

import bmx.bmxaws as bmxaws
import bmx.credentialsutil as credentialsutil

class BmxAwsTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmxaws.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(3, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--account', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--role', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

    @patch('bmx.bmxaws.create_parser')
    @patch('awscli.clidriver.create_clidriver')
    def test_cmd_should_delegate_to_aws_always(self, mock_clidriver, mock_arg_parser):
        class PretendClassDict(dict):
            __getattr__ = dict.get

        ARGS = ['arg']
        KNOWN_ARGS = PretendClassDict(
            {
                'username': 'username',
                'account': 'account',
                'role': 'role'
            }
        )
        UNKNOWN_ARGS = 'unknown args'

        mock_arg_parser.return_value.parse_known_args.return_value = [KNOWN_ARGS,UNKNOWN_ARGS]
        credentialsutil.fetch_credentials = Mock(return_value = {
            'AccessKeyId': 'access key id',
            'SecretAccessKey': 'secret access key',
            'SessionToken': 'session token'
        })
        mock_clidriver.return_value.main.return_value = 0

        self.assertEqual(0, bmxaws.cmd(ARGS))

        mock_arg_parser.return_value.parse_known_args.assert_called_with(ARGS)
        mock_clidriver.return_value.main.assert_called_with(UNKNOWN_ARGS)

    @patch('awscli.clidriver')
    def test_cmd_with_account_and_role_should_pass_correct_args_to_awscli(self, mock_awscli):
        known_args = ['--account', 'my-account',
                      '--username', 'my-user',
                      '--role', 'my-role']
        unknown_args = ['aws_command', 'sub_command']

        bmxaws.cmd(known_args + unknown_args)

        mock_awscli.create_clidriver.return_value.main.assert_called_with(unknown_args)

if __name__ == '__main__':
    unittest.main();
