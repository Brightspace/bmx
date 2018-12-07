package aws

import (
	"strings"

	"github.com/toddradigan/bmx-go/bmx/serviceProviders"
	"github.com/toddradigan/bmx-go/saml"

	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/sts"
	"github.com/aws/aws-sdk-go/service/sts/stsiface"
)

type awsServiceProvider struct {
	STSClient stsiface.STSAPI
}

func NewAWSServiceProvider() (*awsServiceProvider, error) {
	awsSession, err := session.NewSession()
	if err != nil {
		return nil, err
	}

	stsClient := sts.New(awsSession)

	serviceProvider := &awsServiceProvider{
		STSClient: stsClient,
	}

	return serviceProvider, nil
}

func (a awsServiceProvider) GetCredentials(role serviceProviders.Role, saml string) (*sts.Credentials, error) {
	samlInput := &sts.AssumeRoleWithSAMLInput{
		PrincipalArn:  aws.String(role.Principal),
		RoleArn:       aws.String(role.ARN),
		SAMLAssertion: aws.String(saml),
	}

	out, err := a.STSClient.AssumeRoleWithSAML(samlInput)
	if err != nil {
		return nil, err
	}

	return out.Credentials, nil
}

func (a awsServiceProvider) ListRoles(encodedSaml string) ([]serviceProviders.Role, error) {
	samlResponse, err := saml.Decode(encodedSaml)
	if err != nil {
		return nil, err
	}

	roles := make([]serviceProviders.Role, 0)
	for _, v := range samlResponse.Assertion.AttributeStatement.Attributes {
		if v.Name == "https://aws.amazon.com/SAML/Attributes/Role" {
			for _, w := range v.Values {
				splitRole := strings.Split(w, ",")
				role := serviceProviders.Role{}
				role.Principal = splitRole[0]
				role.ARN = splitRole[1]
				role.Name = strings.SplitAfter(role.ARN, "role/")[1]

				roles = append(roles, role)
			}
		}
	}
	return roles, nil
}
