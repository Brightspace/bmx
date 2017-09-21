import unittest

from unittest.mock import patch

import bmx.bmxwrite as bmxwrite
from bmx.locale.options import (BMX_WRITE_USAGE, BMX_WRITE_PROFILE_HELP,
                                BMX_ACCOUNT_HELP, BMX_ROLE_HELP, BMX_USERNAME_HELP)


class BmxWriteTests(unittest.TestCase):
    @patch('argparse.ArgumentParser', autospec=True)
    def test_create_parser_should_create_expected_parser_always(self, mock_argparser_constructor):
        parser = bmxwrite.create_parser()


        mock_argparser_constructor.assert_called_once_with(prog='bmx write', usage=BMX_WRITE_USAGE)
        parser.add_argument.assert_any_call('--username', default=None, help=BMX_USERNAME_HELP)
        parser.add_argument.assert_any_call('--profile', default='default', help=BMX_WRITE_PROFILE_HELP)
        parser.add_argument.assert_any_call('--account', default=None, help=BMX_ACCOUNT_HELP)
        parser.add_argument.assert_any_call('--role', default=None, help=BMX_ROLE_HELP)


if __name__ == '__main__':
    unittest.main()
