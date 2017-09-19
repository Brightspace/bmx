import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

import bmx.bmxprint
import bmx.stsutil
from bmx.aws_credentials import AwsCredentials

ACCESS_KEY_ID = 'id'
SECRET_ACCESS_KEY = 'secret'
SESSION_TOKEN = 'token'
USERNAME = 'username'
DURATION = 'duration'
RETURN_VALUE = AwsCredentials ({
    'AccessKeyId': ACCESS_KEY_ID,
    'SecretAccessKey': SECRET_ACCESS_KEY,
    'SessionToken': SESSION_TOKEN
}, 'expected_account', 'expected_role')

class BmxPrintTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_parser):
        mock_group = Mock()
        mock_parser.return_value.add_mutually_exclusive_group.return_value = mock_group

        bmx.bmxprint.create_parser()

        calls = mock_parser.return_value.add_argument.call_args_list
        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--duration', calls[1][0][0])
        self.assertEqual(3600, calls[1][1]['default'])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--account', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

        self.assertEqual('--role', calls[3][0][0])
        self.assertTrue('help' in calls[3][1])

        calls = mock_group.add_argument.call_args_list
        self.assertEqual('-j', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('-b', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('-p', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

    @patch('bmx.credentialsutil.fetch_credentials')
    @patch('bmx.bmxprint.create_parser')
    def test_cmd_should_print_json_credentials_by_default(self, mock_parser, mock_fetch_credentials):
        for i in [(True, False, False), (False, False, False)]:
            with self.subTest(i=i):
                self.setup_print_mocks(mock_parser, i[0], i[1], i[2], mock_fetch_credentials)

                out = io.StringIO()
                with contextlib.redirect_stdout(out):
                    self.assertEqual(0, bmx.bmxprint.cmd([]))
                out.seek(0)
                printed = json.load(out)

                self.assertEqual(RETURN_VALUE.keys, printed)

    @patch('bmx.credentialsutil.fetch_credentials')
    @patch('bmx.bmxprint.create_parser')
    def test_cmd_should_print_bash_credentials_when_bash_option_specified(self, mock_parser, mock_fetch_credentials):
        self.setup_print_mocks(mock_parser, False, True, False, mock_fetch_credentials)

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(0, bmx.bmxprint.cmd([]))
        printed = out.getvalue()

        self.assertEqual("""export AWS_ACCESS_KEY_ID='{}'
export AWS_SECRET_ACCESS_KEY='{}'
export AWS_SESSION_TOKEN='{}'
""".format(
            ACCESS_KEY_ID,
            SECRET_ACCESS_KEY,
            SESSION_TOKEN
        ), printed)

    @patch('bmx.credentialsutil.fetch_credentials')
    @patch('bmx.bmxprint.create_parser')
    def test_cmd_should_print_powershell_credentials_when_powershell_option_specified(self, mock_parser,
                                                                                      mock_fetch_credentials):
        self.setup_print_mocks(mock_parser, False, False, True, mock_fetch_credentials)

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(0, bmx.bmxprint.cmd([]))
        printed = out.getvalue()

        self.assertEqual("""$env:AWS_ACCESS_KEY_ID = '{}';
$env:AWS_SECRET_ACCESS_KEY = '{}';
$env:AWS_SESSION_TOKEN = '{}'
""".format(
            ACCESS_KEY_ID,
            SECRET_ACCESS_KEY,
            SESSION_TOKEN
        ), printed)

    @patch('builtins.print')
    @patch('bmx.bmxprint.format_credentials')
    @patch('bmx.credentialsutil.fetch_credentials')
    def test_cmd_with_account_and_role_should_pass_correct_args_to_awscli(self, mock_fetch_credentials, *mocks):
        username, duration, account, role = 'my-user', '123', 'my-account', 'my-role'
        known_args = ['--username', username,
                      '--duration', duration,
                      '--account', account,
                      '--role', role]

        bmx.bmxprint.cmd(known_args)
        mock_fetch_credentials.assert_called_with(username=username, duration_seconds=duration, app=account, role=role)

    def setup_print_mocks(self, mock_parser, json, bash, powershell, mock_fetch_credentials=None):
        mock_parser.return_value.parse_known_args.return_value = \
            [self.create_args(json, bash, powershell)]

        if mock_fetch_credentials:
            mock_fetch_credentials.return_value = RETURN_VALUE

    def create_args(self, json, bash, powershell):
        mock = Mock()
        mock.username = USERNAME
        mock.duration = DURATION
        mock.j = json
        mock.b = bash
        mock.p = powershell

        return mock

if __name__ == '__main__':
    unittest.main()
