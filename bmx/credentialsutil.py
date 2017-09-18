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

def read_credentials(app=None, role=None):
    if not app and role or app and not role:
        return None

    if os.path.exists(get_credentials_path()):
        with open(get_credentials_path(), 'r') as credentials_file:
            credentials_doc = yaml.load(credentials_file) or {}

        if not app:
            default_ref = credentials_doc.get(META_KEY, {}).get(DEFAULT_KEY, {})
            app = default_ref.get(app, None)
            role = default_ref.get(role, None)

        credentials = credentials_doc.get(CREDENTIALS_KEY, {}).get(app, {}).get(role, None)

        return AwsCredentials(credentials, app, role) if credentials else None

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
    return read_credentials(app, role) or stsutil.get_credentials(
        username,
        duration_seconds,
        app,
        role)
