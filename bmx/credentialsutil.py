import os

import yaml

import bmx.stsutil as stsutil
from bmx.aws_credentials import AwsCredentials

META_KEY = 'meta'
CREDENTIALS_KEY = 'credentials'
DEFAULT_KEY = 'default'

def create_bmx_path():
    if not os.path.exists(get_bmx_path()):
        os.makedirs(get_bmx_path(), mode=0o770)

def get_bmx_path():
    return os.path.join(os.path.expanduser('~'), '.bmx')

def get_credentials_path():
    return os.path.join(get_bmx_path(), 'credentials')

def get_cookie_session_path():
    return os.path.join(get_bmx_path(), 'cookies.state')

def read_credentials():
    if os.path.exists(get_credentials_path()):
        with open(get_credentials_path(), 'r') as credentials_file:
            credentials_object = yaml.load(credentials_file) or {}
            credentials_dict = credentials_object.get(META_KEY, {})

            #TODO: Get account and role from credentials file
            return AwsCredentials(
                credentials_dict.get(DEFAULT_KEY),
                'get_account_from_file',
                'get_role_from_file')

def write_credentials(credentials):
    create_bmx_path()

    file_descriptor = os.open(
        get_credentials_path(),
        os.O_RDWR | os.O_CREAT,
        mode=0o600
    )

    with open(file_descriptor, 'r+') as credentials_file:
        credentials_object = yaml.load(credentials_file) or {}
        credentials_object.setdefault(META_KEY, {})
        credentials_object[META_KEY][DEFAULT_KEY] = dict(credentials.keys)
        credentials_object[CREDENTIALS_KEY] = credentials.get_dict()

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(credentials_object, credentials_file, default_flow_style=False)


def fetch_credentials(username=None, duration_seconds=3600, app=None, role=None):
    return read_credentials() or stsutil.get_credentials(
        username,
        duration_seconds,
        app,
        role)
