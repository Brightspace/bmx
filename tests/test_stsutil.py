import unittest

from unittest.mock import patch, Mock

from .context import bmx
import bmx.stsutil as stsutil

CREDENTIALS = 'credentials'
DURATION_SECONDS = 'duration seconds'
PRINCIPAL = 'principal'
FIRST_ROLE = 'first-role'
ROLE = 'role'
SAML_ASSERTION = 'saml assertion'
XPATH_RESULTS = 'xpath results'
COOKIES = 'cookies'
ACCOUNT = 'account'
LINK = 'link'

def create_applink_mock(label, appName=None, linkUrl=None):
    return Mock(label=label, appName=appName, linkUrl=linkUrl)

class StsUtilTests(unittest.TestCase):
    @patch('bmx.stsutil.AwsCredentials')
    @patch('bmx.stsutil.sts_assume_role', return_value=CREDENTIALS)
    @patch('bmx.stsutil.get_role_selection', return_value='{0},{1}'.format(FIRST_ROLE, ROLE))
    @patch('bmx.stsutil.get_app_roles')
    @patch('base64.b64decode')
    @patch('bmx.stsutil.get_app_selection', return_value=create_applink_mock(ACCOUNT, linkUrl=LINK))
    @patch('bmx.stsutil.oktautil')
    def test_get_credentials(self, mock_okta, mock_app_selection, mock_base64,
                             mock_get_app_roles, mock_role_selection,
                             mock_assume_role, mock_aws_credentials):
        mock_okta.get_okta_session.return_value = [Mock(userId='user'), COOKIES]
        mock_okta.connect_to_app.return_value = SAML_ASSERTION

        stsutil.get_credentials('username', CREDENTIALS)

        mock_okta.connect_to_app.assert_called_with(LINK, COOKIES)
        mock_assume_role.assert_called_with(SAML_ASSERTION, FIRST_ROLE, ROLE, duration_seconds=CREDENTIALS)
        mock_aws_credentials.assert_called_with(CREDENTIALS, ACCOUNT, ROLE)

    def test_filter_applinks_should_return_sorted_aws_apps(self):
        expected_app_1 = create_applink_mock('app2', 'amazon_aws')
        expected_app_2 = create_applink_mock('app3', 'amazon_aws')
        applinks = [
            expected_app_2,
            create_applink_mock('app1', 'other'),
            expected_app_1
        ]
        self.assertEqual([expected_app_1, expected_app_2], stsutil.filter_applinks(applinks))

    @patch('bmx.prompt.MinMenu.get_selection')
    def test_get_app_selection_should_select_matching_app(self, mock_get_selection):
        expected_applink = create_applink_mock(ACCOUNT)
        self.assertEqual(expected_applink, stsutil.get_app_selection([expected_applink], ACCOUNT))
        self.assertFalse(mock_get_selection.called)

    @patch('bmx.prompt.MinMenu.get_selection', return_value=0)
    def test_get_app_selection_should_prompt_if_app_not_found(self, mock_get_selection):
        expected_applink = create_applink_mock(ACCOUNT)
        self.assertEqual(expected_applink, stsutil.get_app_selection([expected_applink], 'other-account'))
        self.assertTrue(mock_get_selection.called)

    @patch('bmx.prompt.MinMenu.get_selection')
    def test_get_role_selection_should_select_matching_role(self, mock_get_selection):
        expected_role = 'arn::saml-provider/Okta,arn:role/{0}'.format(ROLE)
        self.assertEqual(expected_role, stsutil.get_role_selection(ACCOUNT, [expected_role], ROLE))
        self.assertFalse(mock_get_selection.called)

    @patch('bmx.prompt.MinMenu.get_selection', return_value=0)
    def test_get_role_selection_should_prompt_if_role_not_found(self, mock_get_selection):
        expected_role = 'arn::saml-provider/Okta,arn:role/my-role'
        self.assertEqual(expected_role, stsutil.get_role_selection(ACCOUNT, [expected_role], 'other-role'))
        self.assertTrue(mock_get_selection.called)

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
