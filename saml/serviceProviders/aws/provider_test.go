/*
Copyright 2019 D2L Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package aws_test

import (
	"testing"
	"time"

	"github.com/Brightspace/bmx/mocks"

	"github.com/aws/aws-sdk-go/service/sts/stsiface"

	"github.com/aws/aws-sdk-go/aws"

	awsService "github.com/Brightspace/bmx/saml/serviceProviders/aws"
	"github.com/aws/aws-sdk-go/service/sts"
)

type stsMock struct {
	stsiface.STSAPI
}

func (s *stsMock) AssumeRoleWithSAML(input *sts.AssumeRoleWithSAMLInput) (*sts.AssumeRoleWithSAMLOutput, error) {
	out := &sts.AssumeRoleWithSAMLOutput{
		Credentials: &sts.Credentials{
			AccessKeyId:     aws.String("access_key_id"),
			SecretAccessKey: aws.String("secrest_access_key"),
			SessionToken:    aws.String("session_token"),
			Expiration:      aws.Time(time.Now().Add(time.Hour * 8)),
		},
	}

	return out, nil
}

func TestMonkey(t *testing.T) {
	provider := awsService.NewAwsServiceProvider()
	provider.StsClient = &stsMock{}
	provider.InputReader = mocks.ConsoleReaderMock{}

	// This is a base64 encoded minimal SAML input
	saml := "PHNhbWxwOlJlc3BvbnNlPgogIDxzYW1sOkFzc2VydGlvbj4KICAgIDxzYW1sOkF0dHJpYnV0ZVN0YXRlbWVudD4KICAgICAgPHNhbWw6QXR0cmlidXRlIE5hbWU9Imh0dHBzOi8vYXdzLmFtYXpvbi5jb20vU0FNTC9BdHRyaWJ1dGVzL1JvbGUiPgogICAgICAgIDxzYW1sOkF0dHJpYnV0ZVZhbHVlIHhzaTp0eXBlPSJ4czpzdHJpbmciPkFybixyb2xlL1JvbGVBcm48L3NhbWw6QXR0cmlidXRlVmFsdWU+CiAgICAgIDwvc2FtbDpBdHRyaWJ1dGU+CiAgICA8L3NhbWw6QXR0cmlidXRlU3RhdGVtZW50PgogIDwvc2FtbDpBc3NlcnRpb24+Cjwvc2FtbHA6UmVzcG9uc2U+"

	creds := provider.GetCredentials(saml, "RoleArn", 60)
	if creds == nil {
		panic("fail")
	}
}
