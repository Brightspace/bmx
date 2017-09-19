import os

import yaml
from cerberus import Validator

from bmx.constants import AWS_ACCOUNT_KEY, AWS_ROLE_KEY
from bmx.constants import (BMX_CREDENTIALS_VERSION, BMX_CREDENTIALS_KEY, BMX_DEFAULT_KEY,
                           BMX_META_KEY, BMX_VERSION_KEY)
from bmx.aws_credentials import AwsCredentials


def create_bmx_path():
    if not os.path.exists(get_bmx_path()):
        os.makedirs(get_bmx_path(), mode=0o770)

def get_bmx_path():
    return os.path.join(os.path.expanduser('~'), '.bmx')

def get_credentials_path():
    return os.path.join(get_bmx_path(), 'credentials')

def get_cookie_session_path():
    return os.path.join(get_bmx_path(), 'cookies.state')

def read_credentials(app=None, role=None):
    if not app and role or app and not role:
        return None

    print('Were not mocked')

    if os.path.exists(get_credentials_path()):
        with open(get_credentials_path(), 'r') as credentials_file:
            credentials_doc = yaml.load(credentials_file) or {}

        validate_credentials(credentials_doc)
        if not app and not role:
            default_ref = setdefault(
                setdefault(credentials_doc, BMX_META_KEY), BMX_DEFAULT_KEY)
            app = default_ref.get(AWS_ACCOUNT_KEY)
            role = default_ref.get(AWS_ROLE_KEY)

        credentials = setdefault(
            setdefault(credentials_doc, BMX_CREDENTIALS_KEY), app).get(role)

        return AwsCredentials(credentials, app, role) if credentials else None
    else:
        return None

def write_credentials(credentials):
    create_bmx_path()

    file_descriptor = os.open(
        get_credentials_path(),
        os.O_RDWR | os.O_CREAT,
        mode=0o600
    )

    with open(file_descriptor, 'r+') as credentials_file:
        credentials_doc = yaml.load(credentials_file) or {}
        credentials_doc[BMX_VERSION_KEY] = BMX_CREDENTIALS_VERSION

        setdefault(
            credentials_doc,
            BMX_META_KEY)[BMX_DEFAULT_KEY] = credentials.get_principal_dict()

        setdefault(
            setdefault(
                credentials_doc,
                BMX_CREDENTIALS_KEY),
            credentials.account)[credentials.role] = credentials.keys

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(credentials_doc, credentials_file, default_flow_style=False)

def setdefault(dictionary, key):
    if not isinstance(dictionary.setdefault(key, {}), dict):
        dictionary[key] = {}

    return dictionary[key]

def validate_credentials(credentials):
    schema = {
        'version': {
            'type': 'string',
            'allowed': [BMX_CREDENTIALS_VERSION]
        },
        'meta': {
            'type': 'dict',
            'schema': {
                'default': {
                    'type': 'dict',
                    'required': True,
                    'schema': {
                        'account': {'type': 'string', 'required': True},
                        'role': {'type': 'string', 'required': True}
                    }
                }
            }
        },
        'credentials': {
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
    v = Validator(schema)
    if v.validate(credentials):
        return True
    raise ValueError(v.errors)
