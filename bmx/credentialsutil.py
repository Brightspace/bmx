import os

import yaml

import bmx.stsutil as stsutil

CREDENTIALS_KEY = 'credentials'
DEFAULT_KEY = 'default'

def get_bmx_path():
    return os.path.join(os.path.expanduser('~'), '.bmx')

def get_credentials_path():
    return os.path.join(get_bmx_path(), 'credentials')

def read_credentials():
    if os.path.exists(get_credentials_path()):
        with open(get_credentials_path(), 'r') as credentials_file:
            credentials_object = yaml.load(credentials_file) or {}
            credentials_dict = credentials_object.get(CREDENTIALS_KEY, {})

            return credentials_dict.get(DEFAULT_KEY)

def write_credentials(credentials):
    if not os.path.exists(get_bmx_path()):
        os.makedirs(get_bmx_path(), mode=0o770)

    file_descriptor = os.open(
        get_credentials_path(),
        os.O_RDWR | os.O_CREAT,
        mode=0o600
    )

    with open(file_descriptor, 'r+') as credentials_file:
        credentials_object = yaml.load(credentials_file) or {}
        credentials_object.setdefault(CREDENTIALS_KEY, {})
        credentials_object[CREDENTIALS_KEY][DEFAULT_KEY] = credentials

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(credentials_object, credentials_file, default_flow_style=False)


def fetch_credentials(username=None, duration_seconds=3600):
    return read_credentials() or stsutil.get_credentials(
        username,
        duration_seconds)
