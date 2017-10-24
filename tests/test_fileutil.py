import os
import unittest

from unittest.mock import Mock, patch

from .context import bmx
import bmx.fileutil

PATH = 'path'
DIR = 'dir'

class FileUtilTests(unittest.TestCase):
    @patch('os.open')
    def test_open_file_secure_specifies_expected_args(self, mock_os_open, *args):
        bmx.fileutil.open_file_secure(PATH)

        mock_os_open.assert_called_with(PATH, os.O_TRUNC | os.O_WRONLY | os.O_CREAT, mode=0o600)

    @patch('os.makedirs')
    @patch('os.path.exists', return_value=False)
    def test_create_directory_secure_creates_directory_when_directory_does_not_exist(self,
            mock_os_path_exists,
            mock_os_makedirs):
        bmx.fileutil.create_directory_secure(PATH)

        mock_os_path_exists.assert_called_with(PATH)
        mock_os_makedirs.assert_called_with(PATH, mode=0o770)

    @patch('os.makedirs')
    @patch('os.path.exists', return_value=True)
    def test_create_directory_secure_does_not_create_directory_when_directory_exists(self,
            mock_os_path_exists,
            mock_os_makedirs):
        bmx.fileutil.create_directory_secure(PATH)

        mock_os_path_exists.assert_called_with(PATH)
        mock_os_makedirs.assert_not_called()

    @patch('bmx.fileutil.open_file_secure')
    @patch('bmx.fileutil.create_directory_secure')
    @patch('os.path.dirname', return_value=DIR)
    def test_open_path_secure_calls_collaborators(self,
            mock_os_path_dirname,
            mock_create_directory_secure,
            mock_open_file_secure):

        bmx.fileutil.open_path_secure(PATH)

        mock_os_path_dirname.assert_called_with(PATH)
        mock_create_directory_secure.assert_called_with(DIR)
        mock_open_file_secure.assert_called_with(PATH)

if __name__ == '__main__':
    unittest.main()
