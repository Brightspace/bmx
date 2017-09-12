import argparse
import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

import awscli.clidriver

import bmx.bmxaws
import bmx.bmxwrite

ARGS = ['arg']
KNOWN_ARGS = 'known args'
UNKNOWN_ARGS = 'unknown args'

class BmxAwsTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmx.bmxaws.create_parser()

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
        mock_arg_parser.return_value.parse_known_args.return_value = [KNOWN_ARGS,UNKNOWN_ARGS]
        mock_clidriver.return_value.main.return_value = 0

        self.assertEqual(0, bmx.bmxaws.cmd(ARGS))

        mock_arg_parser.return_value.parse_known_args.assert_called_with(ARGS)
        mock_clidriver.return_value.main.assert_called_with(UNKNOWN_ARGS)



if __name__ == '__main__':
    unittest.main();
