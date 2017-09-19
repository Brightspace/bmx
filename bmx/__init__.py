
from bmx.credentialsutil import read_credentials
from bmx.stsutil import get_credentials

def fetch_credentials(username=None, duration_seconds=3600, app=None, role=None):
    return read_credentials(app, role) or get_credentials(
        username,
        duration_seconds,
        app,
        role)
