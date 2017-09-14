import os

import yaml

import bmx.stsutil as stsutil

def get_bmx_path():
    return os.path.join(os.path.expanduser('~'), '.bmx')

def get_credentials_path():
    return os.path.join(get_bmx_path(), 'credentials')

def read_credentials():
    credentials = None

    if os.path.exists(get_credentials_path()):
        with open(get_credentials_path(), 'r') as credentials_file:
            credentials_object = yaml.load(credentials_file)

            if not isinstance(credentials_object, dict):
                credentials_object = {}

            credentials_dict = credentials_object \
                .get('credentials', {})

            credentials = credentials_dict.get('default')
                
    return credentials

def write_credentials(credentials):
    if not os.path.exists(get_bmx_path()):
        os.makedirs(get_bmx_path(), mode=0o770)

    file_descriptor = os.open(
        get_credentials_path(),
        os.O_RDWR | os.O_CREAT,
        mode=0o600
    )

    with open(file_descriptor, 'r+') as credentials_file:
        credentials_object = yaml.load(credentials_file)

        if not isinstance(credentials_object, dict):
            credentials_object = {}

        credentials_dict = credentials_object.get('credentials', {})
        
        if 'credentials' not in credentials_object:
            credentials_object['credentials'] = {}

        credentials_object['credentials']['default'] = credentials

        credentials_file.seek(0)
        credentials_file.truncate()
        yaml.dump(credentials_object, credentials_file)

def fetch_credentials(username=None, duration_seconds=3600, app=None, role=None):
    credentials = read_credentials()

    if not credentials:
        credentials = stsutil.get_credentials(
            username,
            duration_seconds,
            app=app,
            role=role
        )

    return credentials
