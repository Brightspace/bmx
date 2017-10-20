import unittest

from unittest.mock import Mock
from unittest.mock import patch

from bmx.aws_credentials import AwsCredentials
from bmx.credentialsutil import BmxCredentials
from bmx.constants import (AWS_ACCOUNT_KEY, AWS_ROLE_KEY,
                           BMX_DEFAULT_KEY, BMX_META_KEY, BMX_VERSION_KEY,
                           BMX_CREDENTIALS_KEY, BMX_CREDENTIALS_VERSION)

UNEXPECTED_ACCOUNT = 'unexpected_account'
UNEXPECTED_ROLE = 'unexpected_role'
EXPECTED_ACCOUNT = 'expected_account'
EXPECTED_ROLE = 'expected_role'
UNEXPECTED_ROLE_KEYS = {
    'AccessKeyId': 'invalid',
    'SecretAccessKey': 'invalid',
    'SessionToken': 'invalid'
}
EXPECTED_ROLE_KEYS = {
    'AccessKeyId': 'valid',
    'SecretAccessKey': 'valid',
    'SessionToken': 'valid'
}


class CredentialsUtilTests(unittest.TestCase):
    def check_expected_removal(self, initial_creds, expected_creds, account, role):
        credentials = BmxCredentials(initial_creds)
        result = credentials.remove_credentials(account, role)

        self.assertEqual(result.account, account if account else UNEXPECTED_ACCOUNT)
        self.assertEqual(result.role, role if role else UNEXPECTED_ROLE)
        self.assertDictEqual(UNEXPECTED_ROLE_KEYS, result.keys)

        self.assertDictEqual(expected_creds, credentials.credentials_doc)

    def test_invalid_remove_credentials_input(self):
        for account, role in [(None, 'expected_role'), ('expected_account', None)]:
            with self.assertRaises(ValueError):
                BmxCredentials({}).remove_credentials(account, role)

    def test_validate_should_throw_when_credentials_doc_invalid(self):
        with self.assertRaises(ValueError):
            BmxCredentials({'foo': 'bar'}).validate()

    def test_put_sets_default_and_account_role(self):
        cut = BmxCredentials({})

        cut.put_credentials(AwsCredentials(EXPECTED_ROLE_KEYS,
                EXPECTED_ACCOUNT, EXPECTED_ROLE))

        self.assertEqual({
            BMX_META_KEY: {
                BMX_DEFAULT_KEY: {
                    AWS_ACCOUNT_KEY: EXPECTED_ACCOUNT,
                    AWS_ROLE_KEY: EXPECTED_ROLE
                }
            },
            BMX_CREDENTIALS_KEY: {
                EXPECTED_ACCOUNT: {
                    EXPECTED_ROLE: EXPECTED_ROLE_KEYS
                }
            }}, cut.credentials_doc)

    def test_invalid_get_credentials_input(self):
        for account, role in [(None, 'expected_role'), ('expected_account', None)]:
            self.assertIsNone(BmxCredentials({}).get_credentials(account, role))

    def test_get_credentials_should_use_default_when_account_and_role_are_not_specified(self):
        self.assertEqual(EXPECTED_ROLE_KEYS, BmxCredentials({
            'meta': {
                'default': {
                    'account': EXPECTED_ACCOUNT,
                    'role': EXPECTED_ROLE
                }
            }, 'credentials': {
                EXPECTED_ACCOUNT: {
                    EXPECTED_ROLE: EXPECTED_ROLE_KEYS
                }
            }
        }).get_credentials().keys)

    def test_default_remove_credentials_only_default(self):
        expected_credentials = {
            BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
        }

        for account, role in [(None, None), (UNEXPECTED_ACCOUNT, UNEXPECTED_ROLE)]:
            initial_credentials = {
                BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        AWS_ACCOUNT_KEY: UNEXPECTED_ACCOUNT,
                        AWS_ROLE_KEY: UNEXPECTED_ROLE
                    }
                },
                BMX_CREDENTIALS_KEY: {
                    UNEXPECTED_ACCOUNT: {
                        UNEXPECTED_ROLE:  UNEXPECTED_ROLE_KEYS
                    }
                }
            }
            self.check_expected_removal(initial_credentials, expected_credentials, account, role)

    def test_default_remove_credentials_with_non_default(self):
        expected_credentials = {
            BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
            BMX_CREDENTIALS_KEY: {
                EXPECTED_ACCOUNT: {
                    EXPECTED_ROLE: EXPECTED_ROLE_KEYS
                }
            }
        }

        for account, role in [(None, None), (UNEXPECTED_ACCOUNT, UNEXPECTED_ROLE)]:
            initial_credentials = {
                BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        AWS_ACCOUNT_KEY: UNEXPECTED_ACCOUNT,
                        AWS_ROLE_KEY: UNEXPECTED_ROLE
                    }
                },
                BMX_CREDENTIALS_KEY: {
                    UNEXPECTED_ACCOUNT: {
                        UNEXPECTED_ROLE: UNEXPECTED_ROLE_KEYS
                    },
                    EXPECTED_ACCOUNT: {
                        EXPECTED_ROLE: EXPECTED_ROLE_KEYS
                    }
                }
            }
            self.check_expected_removal(initial_credentials, expected_credentials, account, role)

    def test_remove_credentials_specified_multirole(self):
        expected_credentials = {
            BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
            BMX_CREDENTIALS_KEY: {
                EXPECTED_ACCOUNT: {
                    EXPECTED_ROLE: EXPECTED_ROLE_KEYS
                }
            }
        }

        initial_credentials = {
            BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
            BMX_CREDENTIALS_KEY: {
                EXPECTED_ACCOUNT: {
                    EXPECTED_ROLE: EXPECTED_ROLE_KEYS,
                    UNEXPECTED_ROLE: UNEXPECTED_ROLE_KEYS
                }

            }
        }

        self.check_expected_removal(initial_credentials, expected_credentials, EXPECTED_ACCOUNT, UNEXPECTED_ROLE)

    def test_remove_credentials_invalid_ref(self):
        for account, role in [('invalid_account', 'invalid_role'),
                              (EXPECTED_ACCOUNT, 'invalid_role'),
                              ('invalid_account', EXPECTED_ROLE)]:
            initial_credentials = {
                BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        AWS_ACCOUNT_KEY: EXPECTED_ACCOUNT,
                        AWS_ROLE_KEY: EXPECTED_ROLE
                    }
                },
                BMX_CREDENTIALS_KEY: {
                    EXPECTED_ACCOUNT: {
                        EXPECTED_ROLE: EXPECTED_ROLE_KEYS
                    }
                }
            }
            expected_credentials = dict(initial_credentials)
            credentials = BmxCredentials(initial_credentials)

            result = credentials.remove_credentials(account, role)

            self.assertEqual(result, None)
            self.assertDictEqual(expected_credentials, credentials.credentials_doc)

    @patch('yaml.dump')
    @patch('builtins.open')
    @patch('bmx.fileutil.open_path_secure')
    @patch('bmx.credentialsutil.get_bmx_credentials_path', return_value='/dir/file')
    def test_write_creates_bmx_dir(self,
            mock_get_bmx_credentials_path,
            mock_open_path_secure, *args):

        BmxCredentials({}).write()
        mock_open_path_secure.assert_called_with('/dir/file')
