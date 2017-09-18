#!/usr/bin/python3
import re
import datetime

ACCOUNT_KEY = 'account'
ROLE_KEY = 'role'

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
        return {
            self.account: {
                self.role:
                    {k: v for k, v in self.keys.items()}

            }
        }

    def get_principal_dict(self):
        return {
            ACCOUNT_KEY: self.account,
            ROLE_KEY: self.role
        }
