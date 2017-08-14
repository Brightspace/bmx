#!/usr/bin/python3

import okta
import requests

import inspect
import re
import sys

import html5lib

BASE_URL = 'https://d2l.okta.com/'
API_TOKEN = 'apitoken'

def create_auth_client():
    return okta.AuthClient(
        BASE_URL,
        API_TOKEN,
        headers={'Authorization': None}
    )

def create_sessions_client():
    return okta.SessionsClient(
        BASE_URL,
        API_TOKEN,
        headers={'Authorization': None}
    )

def create_users_client(session_id):
    return okta.UsersClient(
        BASE_URL,
        API_TOKEN,
        headers={
            'Authorization': None,
            'Cookie': 'sid={0}'.format(session_id)
        }
    )

def connect_to_app(app_url, session_token):
    response = requests.get(
        app_url,
        params = {'onetimetoken': session_token}
    )
    response.raise_for_status()

    return html5lib.parse(
        response.text,
        namespaceHTMLElements = False,
    ).find(".//input[@name='SAMLResponse']").get('value')