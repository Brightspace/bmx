import re
from datetime import datetime, timezone
import dateutil

from bmx.constants import AWS_ACCOUNT_KEY, AWS_ROLE_KEY, AWS_EXPIRATION_KEY

class AwsCredentials:
    @staticmethod
    def extract_role_name(role_arn):
        return re.sub('.*:role/', '', role_arn)

    @staticmethod
    def normalize_keys(keys):
        normalized_keys = dict(keys)
        if isinstance(normalized_keys.get(AWS_EXPIRATION_KEY), datetime):
            normalized_keys[AWS_EXPIRATION_KEY] = normalized_keys[AWS_EXPIRATION_KEY].isoformat()

        return normalized_keys

    def __init__(self, keys, account, role_arn):
        self.keys = self.normalize_keys(keys)
        self.account = account
        self.role = self.extract_role_name(role_arn)

    def get_principal_dict(self):
        return {
            AWS_ACCOUNT_KEY: self.account,
            AWS_ROLE_KEY: self.role
        }

    def have_expired(self):
        return AWS_EXPIRATION_KEY in self.keys and \
                dateutil.parser.parse(self.keys[AWS_EXPIRATION_KEY]) <= \
                datetime.now(timezone.utc)
