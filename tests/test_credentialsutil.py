import unittest

import bmx.credentialsutil as credentialsutil
from bmx.constants import BMX_CREDENTIALS_VERSION

class CredentialsUtilTests(unittest.TestCase):
    def test_validate_credentials_success(self):
        test_cases = [
            {},
            {
                'version': BMX_CREDENTIALS_VERSION
            },
            {
                'meta': {
                    'default': {
                        'account': 'testAccount',
                        'role': 'testRole'
                    }
                }
            },
            {
                'credentials': {
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
                'version': BMX_CREDENTIALS_VERSION,
                'meta': {
                    'default': {
                        'account': 'testAccount',
                        'role': 'testRole'
                    }
                },
                'credentials': {
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
                'version': 'bad-version'
            },
            {
                'meta': {
                    'default': {
                        'account': 'missing-role'
                    }
                }
            },
            {
                'credentials': {
                    'testAccount': {
                        'testRole': {
                            'AccessKeyId': '123',
                            'SecretAccessKey': 'missing-token'
                        }
                    }
                }
            },
            {
                'version': BMX_CREDENTIALS_VERSION,
                'meta': {
                    'default': {
                        'account': 'testAccount',
                        'role': 'empty-creds'
                    }
                },
                'credentials': {
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
