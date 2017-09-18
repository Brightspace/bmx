#!/usr/bin/python3
import re
import datetime

class AwsCredentials:
    @staticmethod
    def extract_role_name(role_arn):
        return re.sub('.*:role/', '', role_arn)

    @staticmethod
    def normalize_keys(keys):
        normalized_keys = dict(keys)
        if isinstance(normalized_keys.get('Expiration'), datetime.datetime):
            normalized_keys['Expiration'] = normalized_keys.get('Expiration').isoformat() or None

        return normalized_keys

    def __init__(self, keys, account, role_arn):
        self.keys = self.normalize_keys(keys)
        self.account = account
        self.role = self.extract_role_name(role_arn)

    def get_dict(self):
        return vars(self)
