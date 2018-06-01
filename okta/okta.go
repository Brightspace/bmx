package okta

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"net/http"
	"net/url"
	"strings"
	"time"
)

const (
	applicationJson = "application/json"
)

func NewOktaClient(httpClient *http.Client, org string) (*OktaClient, error) {
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
