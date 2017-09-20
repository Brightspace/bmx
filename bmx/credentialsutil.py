import os
import copy

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

    if os.path.exists(get_credentials_path()):
        with open(get_credentials_path(), 'r') as credentials_file:
            credentials_doc = yaml.load(credentials_file) or {}
        validate_credentials(credentials_doc)


        if not app and not role:
            app, role = get_default_reference(credentials_doc)

        return_value = None
        credentials_dict = credentials_doc.get(BMX_CREDENTIALS_KEY, {}).get(app, {}).get(role)
        if credentials_dict:
            aws_credentials = AwsCredentials(credentials_dict, app, role)
            if not aws_credentials.have_expired():
                return_value = aws_credentials

        return return_value

def write_credentials(credentials):
    create_bmx_path()

    file_descriptor = os.open(
        get_credentials_path(),
        os.O_RDWR | os.O_CREAT,
        mode=0o600
    )

    with open(file_descriptor, 'r+') as credentials_file:
        credentials_doc = yaml.load(credentials_file) or {}
        validate_credentials(credentials_doc)

        credentials_doc[BMX_VERSION_KEY] = BMX_CREDENTIALS_VERSION
        credentials_doc.setdefault(BMX_META_KEY, {})[BMX_DEFAULT_KEY] = \
                credentials.get_principal_dict()

        credentials_doc.setdefault(BMX_CREDENTIALS_KEY, {}) \
                .setdefault(credentials.account, {})[credentials.role] = credentials.keys

        prune_expired(credentials_doc)

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(credentials_doc, credentials_file, default_flow_style=False)

def prune_expired(credentials_doc):
    for app in credentials_doc[BMX_CREDENTIALS_KEY].keys():
        credentials_doc[BMX_CREDENTIALS_KEY][app] = {
                k: v for k, v in credentials_doc[BMX_CREDENTIALS_KEY][app].items() \
                if not AwsCredentials(v, app, k).have_expired()}

    credentials_doc[BMX_CREDENTIALS_KEY] = {
            k: v for k, v in credentials_doc[BMX_CREDENTIALS_KEY].items() \
            if credentials_doc[BMX_CREDENTIALS_KEY][k]}

    app, role = get_default_reference(credentials_doc)
    if not credentials_doc[BMX_CREDENTIALS_KEY].get(app, {}).get(role):
        del credentials_doc[BMX_META_KEY][BMX_DEFAULT_KEY]

    if not credentials_doc[BMX_META_KEY]:
        del credentials_doc[BMX_META_KEY]

    if not credentials_doc[BMX_CREDENTIALS_KEY]:
        del credentials_doc[BMX_CREDENTIALS_KEY]

def get_default_reference(credentials_doc):
    default_ref = credentials_doc.get(BMX_META_KEY, {}).get(BMX_DEFAULT_KEY, {})

    return default_ref.get(AWS_ACCOUNT_KEY), default_ref.get(AWS_ROLE_KEY)

def validate_credentials(credentials):
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
    if validator.validate(credentials):
        return True
    raise ValueError('ERROR: Invalid ~/.bmx/credentials file: {0}'.format(validator.errors))

def remove_default_credentials(credentials_doc):
    app = role = None
    if BMX_META_KEY not in credentials_doc:
        return credentials_doc, app, role

    default_settings = credentials_doc.get(BMX_META_KEY, {}).get(BMX_DEFAULT_KEY, {})
    app = default_settings.get(AWS_ACCOUNT_KEY)
    role = default_settings.get(AWS_ROLE_KEY)

    credentials_doc_no_default = copy.deepcopy(credentials_doc)
    del credentials_doc_no_default[BMX_META_KEY]

    return credentials_doc_no_default, app, role

def remove_named_credentials(credentials_doc, app, role):
    credentials_doc_removed = copy.deepcopy(credentials_doc)
    number_of_account_credentials = len(credentials_doc[BMX_CREDENTIALS_KEY])

    if (app in credentials_doc[BMX_CREDENTIALS_KEY] and
        role in credentials_doc[BMX_CREDENTIALS_KEY][app]):
        number_of_roles_in_account_of_interest = len(credentials_doc[BMX_CREDENTIALS_KEY][app])

        if number_of_account_credentials > 1:
            if number_of_roles_in_account_of_interest > 1:
                del credentials_doc_removed[BMX_CREDENTIALS_KEY][app][role]
            else:
                del credentials_doc_removed[BMX_CREDENTIALS_KEY][app]
        elif number_of_roles_in_account_of_interest > 1:
            del credentials_doc_removed[BMX_CREDENTIALS_KEY][app][role]
        else:
            del credentials_doc_removed[BMX_CREDENTIALS_KEY]

    return credentials_doc_removed

def remove_credentials(app=None, role=None):
    if (not app and role) or (app and not role):
        message = f'Failed to remove credentials.\n' \
                  f'Must specify both account and role or neither.\n' \
                  f'Account: {app}\n' \
                  f'Role: {role}'
        raise ValueError(message)

    if not os.path.exists(get_credentials_path()):
        return

    with open(get_credentials_path(), 'r+') as credentials_file:
        credentials_doc = yaml.load(credentials_file) or {}
        validate_credentials(credentials_doc)

        if not app and not role:
            removed_defaults_doc, app, role = remove_default_credentials(credentials_doc)
            removed_credentials_doc = remove_named_credentials(removed_defaults_doc, app, role)
        else:
            removed_credentials_doc = remove_named_credentials(credentials_doc, app, role)

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(removed_credentials_doc, credentials_file, default_flow_style=False)
