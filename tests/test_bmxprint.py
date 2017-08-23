import argparse
import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

import bmx.bmxprint
import bmx.bmxwrite

USERNAME = 'username'
DURATION = 3
ACCESS_KEY_ID = 'id'
SECRET_ACCESS_KEY = 'secret'
SESSION_TOKEN = 'token'


class BmxPrintTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmx.bmxprint.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(2, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--duration', calls[1][0][0])
        self.assertEqual(3600, calls[1][1]['default'])
        self.assertTrue('help' in calls[1][1])

    @patch('bmx.bmxprint.create_parser')
    def test_cmd_should_print_credentials_always(self, mock_arg_parser):
        return_value = {
            'AccessKeyId': ACCESS_KEY_ID,
            'SecretAccessKey': SECRET_ACCESS_KEY,
            'SessionToken': SESSION_TOKEN
        }

        bmx.bmxwrite.get_credentials = Mock(
            return_value=return_value
        )

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            self.assertEqual(0, bmx.bmxprint.cmd([]))
        out.seek(0)
        printed = json.load(out)

        self.assertEqual(return_value, printed)

if __name__ == '__main__':
    unittest.main();
