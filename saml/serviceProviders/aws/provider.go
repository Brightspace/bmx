package aws

import (
	"encoding/base64"
	"encoding/xml"
	"fmt"
	"log"
	"os"
	"strings"

	"github.com/Brightspace/bmx/console"

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
		StsClient:   stsClient,
		InputReader: console.DefaultConsoleReader{},
		UserOutput:  os.Stderr,
	}
	return serviceProvider
}

type AwsServiceProvider struct {
	StsClient   stsiface.STSAPI
	InputReader console.ConsoleReader
	UserOutput  *os.File
}

func (a AwsServiceProvider) GetCredentials(saml string, desiredRole string) *sts.Credentials {
	decodedSaml, err := base64.StdEncoding.DecodeString(saml)
	if err != nil {
		log.Fatal(err)
	}

	samlResponse := &Saml2pResponse{}
	err = xml.Unmarshal(decodedSaml, samlResponse)
	if err != nil {
		log.Fatal(err)
	}

	var role awsRole
	roles := listRoles(samlResponse)

	if desiredRole == "" {
		role = a.pickRole(roles)
	} else {
		role = findRole(roles, desiredRole)
	}

	samlInput := &sts.AssumeRoleWithSAMLInput{
		PrincipalArn:  aws.String(role.Principal),
		RoleArn:       aws.String(role.ARN),
		SAMLAssertion: aws.String(saml),
	}

	out, err := a.StsClient.AssumeRoleWithSAML(samlInput)
	if err != nil {
		log.Fatal(err)
	}

	return out.Credentials
}

func findRole(roles []awsRole, desiredRole string) awsRole {
	desiredRole = strings.ToLower(desiredRole)
	for _, role := range roles {
		if strings.Compare(strings.ToLower(role.Name), desiredRole) == 0 {
			return role
		}
	}

	log.Fatalf("Unable to find desired role [%s]", desiredRole)
	return awsRole{}
}

func (a AwsServiceProvider) pickRole(roles []awsRole) awsRole {
	if len(roles) == 1 {
		return roles[0]
	}

	for i, role := range roles {
		fmt.Fprintf(a.UserOutput, "[%d] %s\n", i, role.Name)
	}
	j, _ := a.InputReader.ReadInt("Select a role: ")

	return roles[j]
}

func listRoles(samlResponse *Saml2pResponse) []awsRole {
	var roles []awsRole
	for _, v := range samlResponse.Assertion.AttributeStatement.Attributes {
		if v.Name == "https://aws.amazon.com/SAML/Attributes/Role" {
			for _, w := range v.Values {
				splitRole := strings.Split(w, ",")
				role := awsRole{}
				role.Principal = splitRole[0]
				role.ARN = splitRole[1]
				role.Name = strings.SplitAfter(role.ARN, "role/")[1]

				roles = append(roles, role)
			}
		}
	}
	return roles
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
	XMLName xml.Name `xml:"Attribute"`
	Name    string   `xml:"Name,attr""`
	Values  []string `xml:"AttributeValue"`
}

type awsRole struct {
	Name      string
	ARN       string
	Principal string
}
