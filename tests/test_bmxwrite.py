import argparse
import contextlib
import io
import json
import unittest

from unittest.mock import Mock
from unittest.mock import patch

import bmx.bmxprint
import bmx.bmxwrite

CREDENTIALS = 'credentials'
SAML_ASSERTION = 'saml assertion'
PRINCIPAL = 'principal'
ROLE = 'role'
DURATION_SECONDS = 'duration seconds'
XPATH_RESULTS = 'xpath results'


class BmxWriteTests(unittest.TestCase):
    @patch('argparse.ArgumentParser')
    def test_create_parser_should_create_expected_parser_always(self, mock_arg_parser):
        parser = bmx.bmxwrite.create_parser()

        calls = parser.add_argument.call_args_list
        self.assertEqual(1, len(calls))

        self.assertEqual('--username', calls[0][0][0])
        self.assertTrue('help' in calls[0][1])

    @patch('boto3.client')
    def test_sts_assume_role_should_request_token_always(self, mock_sts_client):
        mock_sts_client.return_value.assume_role_with_saml.return_value = {
            'Credentials': CREDENTIALS
        }

        self.assertEqual(
            CREDENTIALS,
            bmx.bmxwrite.sts_assume_role(
                SAML_ASSERTION,
                PRINCIPAL,
                ROLE,
                DURATION_SECONDS
            )
        )

        mock_sts_client.assert_called_with('sts')
        mock_sts_client.return_value.assume_role_with_saml.assert_called_with(
            SAMLAssertion=SAML_ASSERTION,
            PrincipalArn=PRINCIPAL,
            RoleArn=ROLE,
            DurationSeconds=DURATION_SECONDS
        )

    @patch('lxml.etree.fromstring')
    def test_get_app_roles_should_search_saml_for_roles_always(self, mock_etree):
        mock_etree.return_value.xpath.return_value=XPATH_RESULTS

        self.assertEqual(
            XPATH_RESULTS,
            bmx.bmxwrite.get_app_roles(SAML_ASSERTION)
        )

        mock_etree.assert_called_with(SAML_ASSERTION)
        mock_etree.return_value.xpath.assert_called_with(
            ''.join([
                '//x:AttributeStatement',
                '/x:Attribute',
                '[@Name="https://aws.amazon.com/SAML/Attributes/Role"]/x:AttributeValue/text()'
            ]),
            namespaces={'x': 'urn:oasis:names:tc:SAML:2.0:assertion'}
        )

    

    #@patch('bmx.bmxprint.create_parser')
    #def test_cmd_should_print_credentials_always(self, mock_arg_parser):
        #return_value = {
            #'AccessKeyId': ACCESS_KEY_ID,
            #'SecretAccessKey': SECRET_ACCESS_KEY,
            #'SessionToken': SESSION_TOKEN
        #}

        #bmx.bmxwrite.get_credentials = Mock(
            #return_value=return_value
        #)

        #out = io.StringIO()
        #with contextlib.redirect_stdout(out):
            #self.assertEqual(0, bmx.bmxprint.cmd([]))
        #out.seek(0)
        #printed = json.load(out)

        #self.assertEqual(return_value, printed)

if __name__ == '__main__':
    unittest.main();
