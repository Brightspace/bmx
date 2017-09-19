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

from bmx.constants import (BMX_CREDENTIALS_VERSION, BMX_CREDENTIALS_KEY, BMX_VERSION_KEY)


class BmxWriteTests(unittest.TestCase):
    def test_remove_default_credentials_empty(self):
        expected_result = {BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION}
        result, app, role = credentialsutil.remove_default_credentials(expected_result)
        self.assertDictEqual(result, expected_result)

    def test_remove_named_credentials_multiple_account_one_roles(self):
        initial_credentials = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1a': {
                        'k': 'v'
                    }
                },
                'a2': {
                    'r2a': {
                        'k': 'v'
                    }
                }
            }
        }
        expected_result = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1a': {
                        'k': 'v'
                    }
                }
            }
        }
        result = credentialsutil.remove_named_credentials(initial_credentials, 'a2', 'r2a' )
        self.assertDictEqual(result, expected_result)

    def test_remove_named_credentials_one_account_multiple_roles(self):
        initial_credentials = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1a': {
                        'k': 'v'
                    },
                    'r1b': {
                        'k': 'v'
                    }
                }
            }
        }
        expected_result = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1a': {
                        'k': 'v'
                    }
                }
            }
        }
        result = credentialsutil.remove_named_credentials(initial_credentials, 'a1', 'r1b' )
        self.assertDictEqual(result, expected_result)

    def test_remove_named_credentials_one_account_one_role(self):
        initial_credentials = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1a': {
                        'k': 'v'
                    }
                }
            }
        }
        expected_result = {}
        result = credentialsutil.remove_named_credentials(initial_credentials, 'a1', 'r1a' )
        self.assertDictEqual(result, expected_result)

    def test_remove_named_credentials_multiple_account_multiple_roles(self):
        initial_credentials = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1a': {
                        'k': 'v'
                    },
                    'r1b': {
                        'k': 'v'
                    }
                },
                'a2': {
                    'r2a': {
                        'k': 'v'
                    },
                    'r2b': {
                        'k': 'v'
                    }
                }
            }
        }
        expected_result = {
            BMX_CREDENTIALS_KEY: {
                'a1': {
                    'r1b': {
                        'k': 'v'
                    }
                },
                'a2': {
                    'r2a': {
                        'k': 'v'
                    },
                    'r2b': {
                        'k': 'v'
                    }
                }
            }
        }
        result = credentialsutil.remove_named_credentials(initial_credentials, 'a1', 'r1a' )
        self.assertDictEqual(result, expected_result)

    def test_verify_version_error(self):
        with self.assertRaises(ValueError):
            credentialsutil.verify_version({BMX_VERSION_KEY: 'not-a-version'})

    def test_verify_version_pass(self):
        try:
            credentialsutil.verify_version({BMX_VERSION_KEY: BMX_CREDENTIALS_VERSION})
        except Exception:
            self.fail(f'Unexpected exception when testing verify_version({BMX_VERSION_KEY}, {BMX_CREDENTIALS_VERSION})')

if __name__ == '__main__':
    unittest.main()
