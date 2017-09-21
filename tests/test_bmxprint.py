import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

from .context import bmx
import bmx.bmxprint as bmxprint
import bmx.stsutil
from bmx.aws_credentials import AwsCredentials

ACCESS_KEY_ID = 'id'
SECRET_ACCESS_KEY = 'secret'
SESSION_TOKEN = 'token'
USERNAME = 'username'

def create_mock_aws_credentials():
    return Mock(
            **{
                'keys': {
                    'AccessKeyId': ACCESS_KEY_ID,
                    'SecretAccessKey': SECRET_ACCESS_KEY,
                    'SessionToken': SESSION_TOKEN
                }
            })

class BmxPrintTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(
            self, mock_arg_parser_constructor):

        mock_group = Mock()
        mock_parser = mock_arg_parser_constructor.return_value
        mock_parser.add_mutually_exclusive_group.return_value = mock_group

        parser = bmxprint.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--account', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--role', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

        calls = mock_group.add_argument.call_args_list
        self.assertEqual('-j', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('-b', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('-p', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

    @patch('bmx.credentialsutil.load_bmx_credentials')
    @patch('bmx.bmxprint.create_parser')
    def test_cmd_should_print_json_credentials_by_default(self, 
            mock_create_parser, mock_load_bmx_credentials):

        for i in [(True, False, False), (False, False, False)]:
            with self.subTest(i=i):
                self.setup_print_mocks(
                        mock_create_parser.return_value,
                        i[0], i[1], i[2])

                mock_aws_credentials = create_mock_aws_credentials()
                mock_bmx_credentials = mock_load_bmx_credentials.return_value
                mock_bmx_credentials.get_credentials.return_value = mock_aws_credentials

                out = io.StringIO()
                with contextlib.redirect_stdout(out):
                    self.assertEqual(0, bmx.bmxprint.cmd([]))
                out.seek(0)
                printed = json.load(out)

                self.assertEqual(mock_aws_credentials.keys, printed)

                mock_bmx_credentials.get_credentials.assert_called_with(
                        app=None, role=None)
                mock_bmx_credentials.put_credentials.assert_called_with(
                        mock_aws_credentials)
                mock_bmx_credentials.write.assert_called()

    @patch('bmx.credentialsutil.load_bmx_credentials')
    @patch('bmx.bmxprint.create_parser')
    def test_cmd_should_print_bash_and_powershell_credentials_correctly(
            self, mock_create_parser, mock_load_bmx_credentials):

        for i in [
                (False, True, False, """export AWS_ACCESS_KEY_ID='{}'
export AWS_SECRET_ACCESS_KEY='{}'
export AWS_SESSION_TOKEN='{}'
""".format(ACCESS_KEY_ID, SECRET_ACCESS_KEY, SESSION_TOKEN)),
                (False, False, True, """$env:AWS_ACCESS_KEY_ID = '{}';
$env:AWS_SECRET_ACCESS_KEY = '{}';
$env:AWS_SESSION_TOKEN = '{}'
""".format(ACCESS_KEY_ID, SECRET_ACCESS_KEY, SESSION_TOKEN))]:
            with self.subTest(i=i):
                self.setup_print_mocks(
                        mock_create_parser.return_value,
                        i[0], i[1], i[2])

                mock_aws_credentials = create_mock_aws_credentials()
                mock_bmx_credentials = mock_load_bmx_credentials.return_value
                mock_bmx_credentials.get_credentials.return_value = mock_aws_credentials

                out = io.StringIO()
                with contextlib.redirect_stdout(out):
                    self.assertEqual(0, bmx.bmxprint.cmd([]))
                printed = out.getvalue()

                self.assertEqual(i[3], printed)

                mock_bmx_credentials.get_credentials.assert_called_with(
                        app=None, role=None)
                mock_bmx_credentials.put_credentials.assert_called_with(
                        mock_aws_credentials)
                mock_bmx_credentials.write.assert_called()

    @patch('bmx.credentialsutil.load_bmx_credentials')
    def test_cmd_with_account_and_role_should_pass_correct_args(
            self, mock_load_bmx_credentials):

        username, account, role = 'my-user', 'my-account', 'my-role'
        known_args = ['--account', account,
                      '--role', role]

        mock_aws_credentials = create_mock_aws_credentials()
        mock_bmx_credentials = mock_load_bmx_credentials.return_value
        mock_bmx_credentials.get_credentials.return_value = mock_aws_credentials

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(0, bmx.bmxprint.cmd(known_args))

        mock_bmx_credentials.get_credentials.assert_called_with(
                app=account, role=role)
        mock_bmx_credentials.put_credentials.assert_called_with(
                mock_aws_credentials)
        mock_bmx_credentials.write.assert_called()

    def setup_print_mocks(self, mock_parser, json, bash, powershell):
        mock_parser.parse_known_args.return_value = \
            [self.create_mock_args(json, bash, powershell)]

    def create_mock_args(self, json, bash, powershell):
        return Mock(
                **{
                    'username': USERNAME,
                    'account': None,
                    'role': None,
                    'j': json,
                    'b': bash,
                    'p': powershell
                })

if __name__ == '__main__':
    unittest.main()
