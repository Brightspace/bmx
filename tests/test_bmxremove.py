import unittest

from unittest.mock import (Mock, patch)
from bmx.locale.options import (BMX_REMOVE_USAGE,
                                BMX_ACCOUNT_HELP, BMX_ROLE_HELP)

import bmx.bmxremove as bmxremove


class BmxRemoveTests(unittest.TestCase):
    @patch('argparse.ArgumentParser', autospec=True)
    def test_parser_provides_correct_information(self, mock_argparser_constructor):
        parser = bmxremove.create_parser()

        mock_argparser_constructor.assert_called_once_with(prog='bmx remove', usage=BMX_REMOVE_USAGE)
        parser.add_argument.assert_any_call('--account', default=None, help=BMX_ACCOUNT_HELP)
        parser.add_argument.assert_any_call('--role', default=None, help=BMX_ROLE_HELP)


    @patch('bmx.credentialsutil.load_bmx_credentials')
    @patch('bmx.bmxremove.create_parser')
    def test_bmxremove_calls_required_methods(self,
                                              mock_create_parser,
                                              mock_load_bmx_credentials):
        expected_args = 'expected_args'
        expected_account = 'expected_account'
        expected_role = 'expected_role'
        mock_create_parser.return_value.parse_known_args.return_value = [
            Mock(
            ** {
                'account': expected_account,
                'role': expected_role
            }
        )]
        mock_bmx_credentials = Mock()

        mock_load_bmx_credentials.return_value = mock_bmx_credentials


        bmxremove.cmd(expected_args)
        mock_create_parser.return_value.parse_known_args.assert_called_once_with(expected_args)
        mock_load_bmx_credentials.assert_called_once()
        mock_bmx_credentials.remove_credentials.assert_called_once_with(expected_account, expected_role)
        mock_bmx_credentials.write.assert_called_once()
