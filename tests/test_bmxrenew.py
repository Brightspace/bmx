import argparse
import unittest

from unittest.mock import patch

import bmx.bmxrenew as bmxrenew

class BmxRenewTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmxrenew.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(4, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

        self.assertEqual('--duration', calls[1][0][0])
        self.assertTrue('help' in calls[1][1])

        self.assertEqual('--account', calls[2][0][0])
        self.assertTrue('help' in calls[2][1])

        self.assertEqual('--role', calls[3][0][0])
        self.assertTrue('help' in calls[3][1])

if __name__ == '__main__':
    unittest.main();
