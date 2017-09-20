import unittest
from datetime import datetime, timedelta, timezone

from .context import bmx
from bmx.aws_credentials import AwsCredentials

class AwsCredentialsTest(unittest.TestCase):
    def test_normalize_role_with_substituion(self):
        expected_role_name = 'expectedRole'
        role_name = AwsCredentials.extract_role_name(f'arn:aws:iam::123:role/{expected_role_name}')
        self.assertEqual(expected_role_name, role_name)

    def test_normalize_role_without_substituion(self):
        expected_role_name = 'expectedRole'
        role_name = AwsCredentials.extract_role_name(expected_role_name)
        self.assertEqual(expected_role_name, role_name)

    def test_normalize_keys_stringify_expiration(self):
        expected_expiration = '2010-10-10T10:10:10+00:00'
        normalized_keys = \
            AwsCredentials.normalize_keys(
                    {
                        'Expiration': datetime(2010,10,10,10,10,10, tzinfo=timezone.utc)
                    })
        self.assertEqual(expected_expiration, normalized_keys['Expiration'])

    def test_normalize_keys_expiration_same_when_not_datetime(self):
        expected_expiration = 'expected_expiration'
        normalized_keys = \
            AwsCredentials.normalize_keys({'Expiration': expected_expiration})
        self.assertEqual(expected_expiration, normalized_keys['Expiration'])

    def test_are_expired_returns_true_when_expiration_is_in_the_past(self):
        self.assertTrue(AwsCredentials(
            {
                'Expiration': datetime.now(timezone.utc) - timedelta(days=1)
            },
            'expected_account',
            'expected_role').have_expired())

    def test_are_expired_returns_false_when_expiration_is_in_the_future(self):
        self.assertFalse(AwsCredentials(
            {
                'Expiration': datetime.now(timezone.utc) + timedelta(days=1)
            },
            'expected_account',
            'expected_role').have_expired())
