package aws

import (
	"encoding/base64"
	"encoding/xml"
	"log"
	"strings"

	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/aws/session"
	"github.com/aws/aws-sdk-go/service/sts"
	"github.com/aws/aws-sdk-go/service/sts/stsiface"
)

func NewAwsServiceProvider() *AwsServiceProvider {
	awsSession, err := session.NewSession()
	if err != nil {
		log.Fatal(err)
	}

	stsClient := sts.New(awsSession)

	serviceProvider := &AwsServiceProvider{
		StsClient: stsClient,
	}
	return serviceProvider
}

type AwsServiceProvider struct {
	StsClient stsiface.STSAPI
}

func (a AwsServiceProvider) GetCredentials(saml string) *sts.Credentials {
	decSaml, err := base64.StdEncoding.DecodeString(saml)

	samlResponse := &Saml2pResponse{}
	err = xml.Unmarshal(decSaml, samlResponse)
	if err != nil {
		log.Fatal(err)
	}

	var arns []string
	for _, v := range samlResponse.Assertion.AttributeStatement.Attributes {
		if v.Name == "https://aws.amazon.com/SAML/Attributes/Role" {
			arns = strings.Split(v.Value, ",")
		}
	}

	samlInput := &sts.AssumeRoleWithSAMLInput{
		PrincipalArn:  aws.String(arns[0]),
		RoleArn:       aws.String(arns[1]),
		SAMLAssertion: aws.String(saml),
	}

	out, err := a.StsClient.AssumeRoleWithSAML(samlInput)
	if err != nil {
		log.Fatal(err)
	}

	return out.Credentials
}

type Saml2pResponse struct {
	XMLName   xml.Name       `xml:"Response"`
	Assertion Saml2Assertion `xml:"Assertion"`
}

type Saml2Assertion struct {
	XMLName            xml.Name                `xml:"Assertion"`
	AttributeStatement Saml2AttributeStatement `xml:"AttributeStatement"`
}

type Saml2AttributeStatement struct {
	XMLName    xml.Name         `xml:"AttributeStatement"`
	Attributes []Saml2Attribute `xml:"Attribute"`
}

type Saml2Attribute struct {
	Name  string `xml:"Name,attr"`
	Value string `xml:"AttributeValue"`
}
