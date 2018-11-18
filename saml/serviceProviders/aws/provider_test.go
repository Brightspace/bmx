package aws_test

import (
	"testing"
	"time"

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

	// This is a base64 encoded minimal SAML input
	saml := "PHNhbWxwOlJlc3BvbnNlPgogIDxzYW1sOkFzc2VydGlvbj4KICAgIDxzYW1sOkF0dHJpYnV0ZVN0YXRlbWVudD4KICAgICAgPHNhbWw6QXR0cmlidXRlIE5hbWU9Imh0dHBzOi8vYXdzLmFtYXpvbi5jb20vU0FNTC9BdHRyaWJ1dGVzL1JvbGUiPgogICAgICAgIDxzYW1sOkF0dHJpYnV0ZVZhbHVlIHhzaTp0eXBlPSJ4czpzdHJpbmciPkFybixSb2xlQXJuPC9zYW1sOkF0dHJpYnV0ZVZhbHVlPgogICAgICA8L3NhbWw6QXR0cmlidXRlPgogICAgPC9zYW1sOkF0dHJpYnV0ZVN0YXRlbWVudD4KICA8L3NhbWw6QXNzZXJ0aW9uPgo8L3NhbWxwOlJlc3BvbnNlPg=="

	creds := provider.GetCredentials(saml)
	if creds == nil {
		panic("fail")
	}
}
