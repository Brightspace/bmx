import os

import yaml
from cerberus import Validator

from bmx.constants import AWS_ACCOUNT_KEY, AWS_ROLE_KEY
from bmx.constants import (BMX_CREDENTIALS_VERSION, BMX_CREDENTIALS_KEY, BMX_DEFAULT_KEY,
                           BMX_META_KEY, BMX_VERSION_KEY)
from bmx.aws_credentials import AwsCredentials

def get_bmx_path():
    return os.path.join(os.path.expanduser('~'), '.bmx')

def get_bmx_credentials_path():
    return os.path.join(get_bmx_path(), 'credentials')

def get_bmx_cookie_session_path():
    return os.path.join(get_bmx_path(), 'session')

def load_bmx_credentials(credentials_path=get_bmx_credentials_path()):
    if os.path.exists(credentials_path):
        with open(credentials_path, 'r') as credentials_file:
            credentials_doc = yaml.load(credentials_file) or {}
    else:
        credentials_doc = {}

    return BmxCredentials(credentials_doc)

def create_bmx_directory():
    directory = get_bmx_path();

    if not os.path.exists(directory):
        os.makedirs(directory, mode=0o770)

class BmxCredentials:
    def __init__(self, credentials_doc):
        self.credentials_doc = credentials_doc
        self.validate()

    def get_credentials(self, app=None, role=None):
        if (not app and role) or (app and not role):
            return None

        if (not app) and (not role):
            app, role = self.get_default_reference()

        return_value = None
        credentials_dict = self.credentials_doc \
                .get(BMX_CREDENTIALS_KEY, {}).get(app, {}).get(role)
        if credentials_dict:
            aws_credentials = AwsCredentials(credentials_dict, app, role)
            if not aws_credentials.have_expired():
                return_value = aws_credentials

        return return_value

    def put_credentials(self, aws_credentials):
        self.credentials_doc.setdefault(BMX_META_KEY, {})[BMX_DEFAULT_KEY] = \
                aws_credentials.get_principal_dict()

        self.credentials_doc.setdefault(BMX_CREDENTIALS_KEY, {}) \
                .setdefault(aws_credentials.account, {})[aws_credentials.role] = \
                aws_credentials.keys

        self.validate()

    def remove_credentials(self, app=None, role=None):
        if (not app and role) or (app and not role):
            message = f'Failed to remove credentials.\n' \
                      f'Must specify both account and role or neither.\n' \
                      f'Account: {app}\n' \
                      f'Role: {role}'
            raise ValueError(message)

        if (not app) and (not role):
            app, role = self.get_default_reference()

        aws_keys = self.credentials_doc.get(
                BMX_CREDENTIALS_KEY, {}).get(app, {}).pop(role, None)

        self.prune()
        self.validate()

        return AwsCredentials(aws_keys, app, role) if aws_keys else None

    def write(self, credentials_path=get_bmx_credentials_path()):
        directory = os.path.dirname(credentials_path)
        create_bmx_directory()

        self.credentials_doc[BMX_VERSION_KEY] = BMX_CREDENTIALS_VERSION

        self.prune()
        self.validate()

        file_descriptor = os.open(
            credentials_path,
            os.O_CREAT | os.O_WRONLY | os.O_TRUNC,
            mode=0o600
        )

        with open(file_descriptor, 'w') as credentials_file:
            yaml.dump(self.credentials_doc, credentials_file,
                    default_flow_style=False)

    def get_default_reference(self):
        default_ref = self.credentials_doc \
                .get(BMX_META_KEY, {}).get(BMX_DEFAULT_KEY, {})

        return default_ref.get(AWS_ACCOUNT_KEY), default_ref.get(AWS_ROLE_KEY)

    def prune(self):
        if BMX_CREDENTIALS_KEY in self.credentials_doc:
            for app in self.credentials_doc[BMX_CREDENTIALS_KEY].keys():
                self.credentials_doc[BMX_CREDENTIALS_KEY][app] = {
                        k: v for k, v in self.credentials_doc[BMX_CREDENTIALS_KEY][app].items() \
                        if not AwsCredentials(v, app, k).have_expired()}

            self.credentials_doc[BMX_CREDENTIALS_KEY] = {
                    k: v for k, v in self.credentials_doc[BMX_CREDENTIALS_KEY].items() \
                    if self.credentials_doc[BMX_CREDENTIALS_KEY][k]}

            if not self.credentials_doc[BMX_CREDENTIALS_KEY]:
                del self.credentials_doc[BMX_CREDENTIALS_KEY]

        if BMX_META_KEY in self.credentials_doc:
            if BMX_DEFAULT_KEY in self.credentials_doc[BMX_META_KEY]:
                app, role = self.get_default_reference()
                if not self.credentials_doc.get(BMX_CREDENTIALS_KEY, {}).get(app, {}).get(role):
                    del self.credentials_doc[BMX_META_KEY][BMX_DEFAULT_KEY]

            if not self.credentials_doc[BMX_META_KEY]:
                del self.credentials_doc[BMX_META_KEY]

    def validate(self):
        schema = {
            BMX_VERSION_KEY: {
                'type': 'string',
                'allowed': [BMX_CREDENTIALS_VERSION]
            },
            BMX_META_KEY: {
                'type': 'dict',
                'schema': {
                    BMX_DEFAULT_KEY: {
                        'type': 'dict',
                        'required': True,
                        'schema': {
                            'account': {'type': 'string', 'required': True},
                            'role': {'type': 'string', 'required': True}
                        }
                    }
                }
            },
            BMX_CREDENTIALS_KEY: {
                'type': 'dict',
                'minlength': 1,
                'valueschema': {
                    'type': 'dict',
                    'minlength': 1,
                    'valueschema': {
                        'type': 'dict',
                        'schema': {
                            'AccessKeyId': {'type': 'string', 'required': True},
                            'SecretAccessKey': {'type': 'string', 'required': True},
                            'SessionToken': {'type': 'string', 'required': True},
                            'Expiration': {'type': 'string'}
                        }
                    }
                }
            }
        }

        validator = Validator(schema)
        if validator.validate(self.credentials_doc):
            return True
        raise ValueError('ERROR: Invalid ~/.bmx/credentials file: {0}'.format(validator.errors))
