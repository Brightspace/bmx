# bmx

## 1.0.0
### FEATURES
* BMX is multi-platform: it runs on Linux and Windows.
* BMX maintains your Okta session for 12 hours: you enter your Okta password once a day, and BMX takes care of the rest.
* BMX supports TOTP and SMS MFA.
* BMX manages its own AWS STS tokens and never modifies ~/.aws/credentials without explicit direction from you. (See bmx write.)
* BMX has commands for renewing and forgetting its AWS STS tokens.
* BMX manages its AWS-CLI dependency.

