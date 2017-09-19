import unittest

import bmx.credentialsutil as credentialsutil
from bmx.constants import (BMX_CREDENTIALS_VERSION, BMX_CREDENTIALS_KEY,
                           BMX_DEFAULT_KEY, BMX_META_KEY, BMX_VERSION_KEY)

class CredentialsUtilTests(unittest.TestCase):
    def test_validate_credentials_success(self):
        test_cases = [
            {},
            {
                BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION
            },
            {
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        'account': 'testAccount',
                        'role': 'testRole'
                    }
                }
            },
            {
                BMX_CREDENTIALS_KEY: {
                    'testAccount': {
                        'testRole': {
                            'AccessKeyId': '123',
                            'SecretAccessKey': 'secret',
                            'SessionToken': 'session'
                        }
                    }
                }
            },
            {
                BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        'account': 'testAccount',
                        'role': 'testRole'
                    }
                },
                BMX_CREDENTIALS_KEY: {
                    'testAccount': {
                        'testRole': {
                            'AccessKeyId': 'accessKey',
                            'SecretAccessKey': 'secret',
                            'SessionToken': 'session',
                            'Expiration': 'expiration'
                        }
                    }
                }
            }
        ]
        for test in test_cases:
            with self.subTest(test=test):
                self.assertTrue(credentialsutil.validate_credentials(test))

    def test_validate_credentials_failure(self):
        test_cases = [
            {
                BMX_VERSION_KEY: 'bad-version'
            },
            {
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        'account': 'missing-role'
                    }
                }
            },
            {
                BMX_CREDENTIALS_KEY: {
                    'testAccount': {
                        'testRole': {
                            'AccessKeyId': '123',
                            'SecretAccessKey': 'missing-token'
                        }
                    }
                }
            },
            {
                BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION,
                BMX_META_KEY: {
                    BMX_DEFAULT_KEY: {
                        'account': 'testAccount',
                        'role': 'empty-creds'
                    }
                },
                BMX_CREDENTIALS_KEY: {
                    'testAccount': {
                        'empty-creds': {}
                    }
                }
            }
        ]
        for test in test_cases:
            with self.subTest(test=test):
                self.assertRaises(ValueError, credentialsutil.validate_credentials, test)

if __name__ == '__main__':
    unittest.main()
