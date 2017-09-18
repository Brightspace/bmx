import unittest
import datetime
from dateutil.tz import tzutc

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
            AwsCredentials.normalize_keys({'Expiration': datetime.datetime(2010,10,10,10,10,10, tzinfo=tzutc())})
        self.assertEqual(expected_expiration, normalized_keys['Expiration'])

    def test_normalize_keys_expiration_same_when_not_datetime(self):
        expected_expiration = 'expected_expiration'
        normalized_keys = \
            AwsCredentials.normalize_keys({'Expiration': expected_expiration})
        self.assertEqual(expected_expiration, normalized_keys['Expiration'])

    def test_get_dict_from_credentials(self):
        credentials = AwsCredentials(
            {'expected_key': 'expected_value'},
            'expected_account',
            'expected_role')
        self.assertDictEqual({
            'expected_account': {
                'expected_role': {
                    'expected_key': 'expected_value'
                }
            }
        }, credentials.get_dict())