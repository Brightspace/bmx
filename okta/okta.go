package okta

import (
	"bytes"
	"encoding/base64"
	"encoding/json"
	"encoding/xml"
	"fmt"
	"io"
	"io/ioutil"
	"log"
	"net/http"
	"net/http/cookiejar"
	"net/url"
	"strings"
	"time"

	"golang.org/x/net/html"

	"golang.org/x/net/publicsuffix"
)

const (
	applicationJson = "application/json"
)

func NewOktaClient(org string) (*OktaClient, error) {
	// All users of cookiejar should import "golang.org/x/net/publicsuffix"
	jar, err := cookiejar.New(&cookiejar.Options{PublicSuffixList: publicsuffix.List})
	if err != nil {
		log.Fatal(err)
	}
	httpClient := &http.Client{
		Timeout: 30 * time.Second,
		Jar:     jar,
	}

	client := &OktaClient{
		HttpClient: httpClient,
	}

	client.BaseUrl, _ = url.Parse(fmt.Sprintf("https://%s.okta.com/api/v1/", org))

	return client, nil
}

type OktaClient struct {
	HttpClient *http.Client
	BaseUrl    *url.URL
}

func (o *OktaClient) GetHttpClient() *http.Client {
	return o.HttpClient
}

func (o *OktaClient) GetBaseUrl() *url.URL {
	return o.BaseUrl
}

func (o *OktaClient) Authenticate(username string, password string) (*OktaAuthResponse, error) {
	rel, err := url.Parse("authn")
	if err != nil {
		return nil, err
	}

	url := o.BaseUrl.ResolveReference(rel)
	if err != nil {
		return nil, err
	}

	body := fmt.Sprintf(`{"username":"%s", "password":"%s"}`, username, password)
	authResponse, err := o.HttpClient.Post(url.String(), applicationJson, strings.NewReader(body))
	if err != nil {
		return nil, err
	}

	oktaAuthResponse := &OktaAuthResponse{}
	z, err := ioutil.ReadAll(authResponse.Body)
	err = json.Unmarshal(z, &oktaAuthResponse)

	return oktaAuthResponse, err
}

func (o *OktaClient) StartSession(sessionToken string) (*OktaSessionResponse, error) {
	rel, err := url.Parse("sessions")
	if err != nil {
		return nil, err
	}
	url := o.BaseUrl.ResolveReference(rel)
	if err != nil {
		return nil, err
	}
	oktaSessionsRequest := OktaSessionsRequest{
		SessionToken: sessionToken,
	}
	b, err := json.Marshal(oktaSessionsRequest)
	sessionResponse, err := o.HttpClient.Post(url.String(), applicationJson, bytes.NewReader(b))

	oktaSessionResponse := &OktaSessionResponse{}
	b, err = ioutil.ReadAll(sessionResponse.Body)
	err = json.Unmarshal(b, oktaSessionResponse)
	if err != nil {
		return nil, err
	}

	return oktaSessionResponse, nil
}

func (o *OktaClient) ListApplications(userId string) ([]OktaAppLink, error) {
	rel, _ := url.Parse(fmt.Sprintf("users/%s/appLinks", userId))
	url := o.BaseUrl.ResolveReference(rel)

	listApplicationRequest, err := http.NewRequest("GET", url.String(), nil)
	listApplicationsResponse, err := o.HttpClient.Do(listApplicationRequest)
	if err != nil {
		return nil, err
	}
	var oktaApplications []OktaAppLink
	b, err := ioutil.ReadAll(listApplicationsResponse.Body)
	err = json.Unmarshal(b, &oktaApplications)
	if err != nil {
		return nil, err
	}

	return oktaApplications, nil
}

func (o *OktaClient) SetSessionId(id string) {
	cookies := o.HttpClient.Jar.Cookies(o.BaseUrl)
	cookie := &http.Cookie{
		Name:     "sid",
		Value:    id,
		Path:     "/",
		Domain:   o.BaseUrl.Host,
		Secure:   true,
		HttpOnly: true,
	}
	cookies = append(cookies, cookie)
	o.HttpClient.Jar.SetCookies(o.BaseUrl, cookies)
}

func (o *OktaClient) GetSaml(appLink OktaAppLink) (Saml2pResponse, string, error) {
	appResponse, err := o.HttpClient.Get(appLink.LinkUrl)
	if err != nil {
		log.Fatal(err)
	}

	saml, err := GetSaml(appResponse.Body)
	decSaml, err := base64.StdEncoding.DecodeString(saml)

	samlResponse := &Saml2pResponse{}
	err = xml.Unmarshal(decSaml, samlResponse)
	if err != nil {
		log.Fatal(err)
	}

	return *samlResponse, saml, nil
}

type OktaAuthResponse struct {
	ExpiresAt    time.Time                `json:"expiresAt"`
	SessionToken string                   `json:"sessionToken"`
	StateToken   string                   `json:"stateToken"`
	Status       string                   `json:"status"`
	Embedded     OktaAuthResponseEmbedded `json:"_embedded"`
}

type OktaAuthResponseEmbedded struct {
	Factors []OktaAuthFactors `json:"factors"`
}

type OktaAuthFactors struct {
	Id         string    `json:"id"`
	FactorType string    `json:"factorType"`
	Links      OktaLinks `json:"_links"`
}

type OktaLinks struct {
	Verify OktaVerifyLink `json:"verify"`
}

type OktaVerifyLink struct {
	Url string `json:"href"`
}

type OktaSessionsRequest struct {
	SessionToken string `json:"sessionToken"`
}

type OktaSessionResponse struct {
	Id     string `json:"id"`
	UserId string `json:"userId"`
}

type OktaAppLink struct {
	Id            string `json:"id"`
	Label         string `json:"label"`
	LinkUrl       string `json:"linkUrl"`
	AppName       string `json:"appName"`
	AppInstanceId string `json:"appInstanceId"`
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

func GetSaml(r io.Reader) (string, error) {
	z := html.NewTokenizer(r)
	for {
		tt := z.Next()
		switch tt {
		case html.ErrorToken:
			return "", z.Err()
		case html.SelfClosingTagToken:
			tn, hasAttr := z.TagName()

			if string(tn) == "input" {
				attr := make(map[string]string)
				for hasAttr {
					key, val, moreAttr := z.TagAttr()
					attr[string(key)] = string(val)
					if !moreAttr {
						break
					}
				}

				if attr["name"] == "SAMLResponse" {
					return string(attr["value"]), nil
				}
			}
		}
	}

	return "", nil
}
