import unittest

from unittest.mock import patch

from .context import bmx
import bmx.stsutil as stsutil

CREDENTIALS = 'credentials'
DURATION_SECONDS = 'duration seconds'
PRINCIPAL = 'principal'
ROLE = 'role'
SAML_ASSERTION = 'saml assertion'
XPATH_RESULTS = 'xpath results'

class StsUtilTests(unittest.TestCase):
    @patch('lxml.etree.fromstring')
    def test_get_app_roles_should_search_saml_for_roles_always(self, mock_etree):
        mock_etree.return_value.xpath.return_value=XPATH_RESULTS

        self.assertEqual(
            XPATH_RESULTS,
            stsutil.get_app_roles(SAML_ASSERTION)
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

    @patch('boto3.client')
    def test_sts_assume_role_should_request_token_always(self, mock_sts_client):
        mock_sts_client.return_value.assume_role_with_saml.return_value = {
            'Credentials': CREDENTIALS
        }

        self.assertEqual(
            CREDENTIALS,
            stsutil.sts_assume_role(
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

if __name__ == '__main__':
    unittest.main();
