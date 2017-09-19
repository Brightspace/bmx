import unittest

import bmx.credentialsutil as credentialsutil


class BmxWriteTests(unittest.TestCase):
    def test_set_default_empty(self):
        actual = credentialsutil.setdefault({}, 'key')
        self.assertDictEqual(actual, {})

    def test_set_default_non_empty_value(self):
        actual = credentialsutil.setdefault({'key': 'value'}, 'key')
        self.assertDictEqual(actual, {})

    def test_set_default_non_empty_dict(self):
        expected = {'key2': 'value'}
        actual = credentialsutil.setdefault({'key': {'key2': 'value'}}, 'key')
        self.assertDictEqual(actual, expected)