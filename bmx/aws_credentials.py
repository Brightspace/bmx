#!/usr/bin/python3
import re


class AwsCredentials:
    @staticmethod
    def extract_role_name(role_arn):
        return re.sub('.*:role/', '', role_arn)

    @staticmethod
    def normalize_keys(keys):
        norm_keys = dict(keys)
        norm_keys['Expiration'] = str(norm_keys.get('Expiration'))
        return norm_keys

    def __init__(self, keys, account, role_arn):
        self.keys = self.normalize_keys(keys)
        self.account = account
        self.role = self.extract_role_name(role_arn)

    def get_dict(self):
        return vars(self)
