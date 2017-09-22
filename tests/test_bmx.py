import unittest

from unittest.mock import (Mock, patch)


import bmx.bmx as bmx_main
from bmx.locale.options import (BMX_COPYRIGHT, BMX_DESCRIPTION,
                                BMX_AWS_HELP, BMX_PRINT_HELP, BMX_REMOVE_HELP,
                                BMX_RENEW_HELP, BMX_WRITE_HELP)

class BmxMainTest(unittest.TestCase):
    @patch('argparse.ArgumentParser', autospec=True)
    def test_parser_provides_correct_information(self, mock_argparser_constructor):
        self.assertEqual(1, bmx_main.main())
        mock_argparser_constructor.assert_called_once_with(add_help=False,
                                                           description=BMX_DESCRIPTION,
                                                           epilog=BMX_COPYRIGHT)

        mock_parser = mock_argparser_constructor.return_value
        mock_parser.add_subparsers.assert_called_once_with(title='commands')

        mock_sub_parser = mock_parser.add_subparsers.return_value
        mock_sub_parser.add_parser.assert_any_call('aws', help=BMX_AWS_HELP, add_help=False)
        mock_sub_parser.add_parser.assert_any_call('write', help=BMX_WRITE_HELP, add_help=False)
        mock_sub_parser.add_parser.assert_any_call('print', help=BMX_PRINT_HELP, add_help=False)
        mock_sub_parser.add_parser.assert_any_call('renew', help=BMX_RENEW_HELP, add_help=False)
        mock_sub_parser.add_parser.assert_any_call('remove', help=BMX_REMOVE_HELP, add_help=False)


    @patch('bmx.bmx.Parser', autospec=True)
    def test_usage_we(self, mock_parser):
        with patch('sys.argv', ['bmx']):
            self.assertEqual(2, bmx_main.main())
            mock_parser.return_value.usage.assert_called_once()
