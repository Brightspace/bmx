package mocks

import (
	"time"

	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/service/sts"
)

type AwsServiceProviderMock struct{}

func (a *AwsServiceProviderMock) GetCredentials(saml string) *sts.Credentials {
	return &sts.Credentials{
		AccessKeyId:     aws.String("access_key_id"),
		SecretAccessKey: aws.String("secrest_access_key"),
		SessionToken:    aws.String("session_token"),
		Expiration:      aws.Time(time.Now().Add(time.Hour * 8)),
	}
}
