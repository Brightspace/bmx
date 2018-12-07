package saml

import (
	"encoding/base64"
	"encoding/xml"
)

type Saml2Assertion struct {
	XMLName            xml.Name                `xml:"Assertion"`
	AttributeStatement Saml2AttributeStatement `xml:"AttributeStatement"`
}

type Saml2Attribute struct {
	XMLName xml.Name `xml:"Attribute"`
	Name    string   `xml:"Name,attr"`
	Values  []string `xml:"AttributeValue"`
}

type Saml2AttributeStatement struct {
	XMLName    xml.Name         `xml:"AttributeStatement"`
	Attributes []Saml2Attribute `xml:"Attribute"`
}

type Saml2pResponse struct {
	XMLName   xml.Name       `xml:"Response"`
	Assertion Saml2Assertion `xml:"Assertion"`
}

func Decode(saml string) (*Saml2pResponse, error) {
	decodedSaml, err := base64.StdEncoding.DecodeString(saml)
	if err != nil {
		return nil, err
	}

	samlResponse := &Saml2pResponse{}
	if err := xml.Unmarshal(decodedSaml, samlResponse); err != nil {
		return nil, err
	}

	return samlResponse, nil
}
