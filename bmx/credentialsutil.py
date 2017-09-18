import os

import yaml

import bmx.stsutil as stsutil
from bmx.aws_credentials import AwsCredentials

VERSION_KEY = 'version'
VERSION = '1.0.0'
META_KEY = 'meta'
CREDENTIALS_KEY = 'credentials'
DEFAULT_KEY = 'default'
ACCOUNT_KEY = 'account'
ROLE_KEY = 'role'

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

        version = credentials_doc.get(VERSION_KEY)
        if version and version != VERSION:
            message = (
                'Invalid credentials version.'
                '\n   Supported: {0}'
                '\n   Current: {1}'
            ).format(VERSION, version)
            raise ValueError(message)

        if not app and not role:
            default_ref = setdefault(
                setdefault(credentials_doc, META_KEY), DEFAULT_KEY)
            app = default_ref.get(ACCOUNT_KEY)
            role = default_ref.get(ROLE_KEY)

        credentials = setdefault(
            setdefault(credentials_doc, CREDENTIALS_KEY), app).get(role)

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
        credentials_object[VERSION_KEY] = VERSION

        setdefault(
            credentials_doc,
            META_KEY)[DEFAULT_KEY] = credentials.get_principal_dict()

        setdefault(
            setdefault(
                credentials_doc,
                CREDENTIALS_KEY),
            credentials.account)[credentials.role] = credentials.keys

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(credentials_doc, credentials_file, default_flow_style=False)

def setdefault(dictionary, key):
    if not isinstance(dictionary.get(key, {}), dict):
        dictionary[key] = {}

    return dictionary[key]

def fetch_credentials(username=None, duration_seconds=3600, app=None, role=None):
    return read_credentials(app, role) or stsutil.get_credentials(
        username,
        duration_seconds,
        app,
        role)
