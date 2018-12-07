package okta

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"net/http"
	"net/http/cookiejar"
	"net/url"
	"strings"
	"time"

	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/saml"

	"golang.org/x/net/publicsuffix"
)

const (
	applicationJSON = "application/json"
)

type errorResponse struct {
	Code    string         `json:"errorCode"`
	Summary string         `json:"errorSummary"`
	Link    string         `json:"errorLink"`
	ErrorID string         `json:"errorId"`
	Causes  []errorSummary `json:"errorCauses"`
}

type errorSummary struct {
	Summary string `json:"errorSummary"`
}

type oktaAppLink struct {
	ID            string `json:"id"`
	Label         string `json:"label"`
	LinkURL       string `json:"linkUrl"`
	AppName       string `json:"appName"`
	AppInstanceID string `json:"appInstanceId"`
}

type oktaAuthFactors struct {
	ID         string    `json:"id"`
	FactorType string    `json:"factorType"`
	Links      oktaLinks `json:"_links"`
}

type oktaAuthResponse struct {
	ExpiresAt    time.Time                `json:"expiresAt"`
	SessionToken string                   `json:"sessionToken"`
	StateToken   string                   `json:"stateToken"`
	Status       string                   `json:"status"`
	Embedded     oktaAuthResponseEmbedded `json:"_embedded"`
}

type oktaAuthResponseEmbedded struct {
	Factors []oktaAuthFactors `json:"factors"`
}

type oktaClient struct {
	authStateToken string
	httpClient     *http.Client
	baseURL        *url.URL
	factors        []identityProviders.MFAFactor
	sessionCache   sessionCache
}

type oktaLinks struct {
	Verify oktaVerifyLink `json:"verify"`
}

type oktaMeResponse struct {
	ID string `json:"id"`
}

type oktaSessionCache struct {
	Username  string `json:"userId"`
	SessionID string `json:"sessionId"`
	ExpiresAt string `json:"expiresAt"`
}

type oktaSessionsRequest struct {
	SessionToken string `json:"sessionToken"`
}

type oktaSessionResponse struct {
	ID        string `json:"id"`
	UserID    string `json:"userId"`
	ExpiresAt string `json:"expiresAt"`
}

type oktaVerifyLink struct {
	URL string `json:"href"`
}

type sessionCache interface {
	Save([]oktaSessionCache) error
	Sessions() ([]oktaSessionCache, error)
}

func NewOktaClient(org string) (*oktaClient, error) {
	// All users of cookiejar should import "golang.org/x/net/publicsuffix"
	jar, err := cookiejar.New(&cookiejar.Options{PublicSuffixList: publicsuffix.List})
	if err != nil {
		return nil, err
	}

	httpClient := &http.Client{
		Timeout: 30 * time.Second,
		Jar:     jar,
	}

	url, err := url.Parse(fmt.Sprintf("https://%s.okta.com/api/v1/", org))
	if err != nil {
		return nil, err
	}

	cache := NewOktaSessionFileCache(org)

	client := &oktaClient{
		httpClient:   httpClient,
		baseURL:      url,
		sessionCache: cache,
	}

	return client, nil
}

func (o *oktaClient) Authenticate(username, password string) (string, error) {
	o.authStateToken = ""
	o.factors = nil

	rel, err := url.Parse("authn")
	if err != nil {
		return "", err
	}

	url := o.baseURL.ResolveReference(rel)
	if err != nil {
		return "", err
	}

	body := fmt.Sprintf(`{"username":"%s", "password":"%s"}`, username, password)
	authResponse, err := o.httpClient.Post(url.String(), applicationJSON, strings.NewReader(body))
	if err != nil {
		return "", err
	}

	if authResponse.StatusCode != 200 {
		z, err := ioutil.ReadAll(authResponse.Body)
		if err != nil {
			return "", err
		}

		eResp := &errorResponse{}
		if err := json.Unmarshal(z, &eResp); err != nil {
			return "", fmt.Errorf("Received invalid response from okta.\nReponse code: %q\nBody:%s", authResponse.Status, body)
		}

		return "", fmt.Errorf("%s. Response code: %q", eResp.Summary, authResponse.Status)
	}

	oktaAuthResponse := &oktaAuthResponse{}
	z, err := ioutil.ReadAll(authResponse.Body)
	if err != nil {
		return "", err
	}

	if err := json.Unmarshal(z, &oktaAuthResponse); err != nil {
		return "", err
	}

	if oktaAuthResponse.Status == "MFA_REQUIRED" {
		o.authStateToken = oktaAuthResponse.StateToken
		factors := make([]identityProviders.MFAFactor, 0)
		for _, factor := range oktaAuthResponse.Embedded.Factors {
			factors = append(factors, identityProviders.MFAFactor{
				Factor: factor.FactorType,
				URL:    factor.Links.Verify.URL,
			})
		}

		o.factors = factors

		return "", fmt.Errorf(oktaAuthResponse.Status)
	}

	oktaSessionResponse, err := o.startSession(oktaAuthResponse.SessionToken)
	if err != nil {
		return "", err
	}

	o.setSessionID(oktaSessionResponse.ID)
	err = o.cacheSession(username, oktaSessionResponse.ID, oktaSessionResponse.ExpiresAt)
	if err != nil {
		fmt.Println("[failed to cache session]")
		fmt.Println(err)
	}

	return oktaSessionResponse.UserID, nil
}

func (o *oktaClient) AuthenticateFromCache(username string) (string, bool, error) {
	currTime := time.Now()

	cachedSessions, err := o.sessionCache.Sessions()
	if err != nil {
		return "", false, err
	}

	activeSessions := make([]oktaSessionCache, 0)
	for _, cachedSession := range cachedSessions {
		expireTime, err := time.Parse(time.RFC3339, cachedSession.ExpiresAt)
		if err == nil && expireTime.After(currTime) {
			activeSessions = append(activeSessions, cachedSession)
		}
	}

	if len(activeSessions) == 0 {
		return "", false, nil
	}

	sessionFound := false
	sessionID := ""
	for _, session := range activeSessions {
		if session.Username == username {
			sessionFound = true
			sessionID = session.SessionID
			break
		}
	}

	if !sessionFound {
		return "", false, nil
	}

	o.setSessionID(sessionID)

	rel, err := url.Parse(fmt.Sprintf("users/me"))
	if err != nil {
		return "", false, err
	}

	url := o.baseURL.ResolveReference(rel)

	meRequest, err := http.NewRequest("GET", url.String(), nil)
	if err != nil {
		return "", false, err
	}

	meResponse, err := o.httpClient.Do(meRequest)
	if err != nil {
		return "", false, err
	}

	var me oktaMeResponse
	b, err := ioutil.ReadAll(meResponse.Body)
	if err != nil {
		return "", false, err
	}

	err = json.Unmarshal(b, &me)
	if err != nil {
		return "", false, err
	}

	return me.ID, true, nil
}

func (o *oktaClient) CompleteMFA(username, url, mfaCode string) (string, error) {
	body := fmt.Sprintf(`{"stateToken":"%s"}`, o.authStateToken)
	authResponse, err := o.httpClient.Post(url, applicationJSON, strings.NewReader(body))
	if err != nil {
		return "", err
	}

	oktaAuthResponse := &oktaAuthResponse{}
	z, err := ioutil.ReadAll(authResponse.Body)
	if err != nil {
		return "", err
	}

	err = json.Unmarshal(z, &oktaAuthResponse)
	if err != nil {
		return "", err
	}

	body = fmt.Sprintf(`{"stateToken":"%s","passCode":"%s"}`, oktaAuthResponse.StateToken, mfaCode)
	authResponse, err = o.httpClient.Post(url, applicationJSON, strings.NewReader(body))
	if err != nil {
		return "", err
	}

	z, err = ioutil.ReadAll(authResponse.Body)
	if err != nil {
		return "", err
	}

	if err := json.Unmarshal(z, &oktaAuthResponse); err != nil {
		return "", err
	}

	oktaSessionResponse, err := o.startSession(oktaAuthResponse.SessionToken)
	if err != nil {
		return "", err
	}

	o.setSessionID(oktaSessionResponse.ID)
	err = o.cacheSession(username, oktaSessionResponse.ID, oktaSessionResponse.ExpiresAt)
	if err != nil {
		fmt.Println("[failed to cache session]")
		fmt.Println(err)
	}

	return oktaSessionResponse.UserID, nil
}

func (o *oktaClient) GetMFAFactors() ([]identityProviders.MFAFactor, error) {
	if o.factors == nil {
		return []identityProviders.MFAFactor{}, nil
	}

	return o.factors, nil
}

func (o *oktaClient) GetSaml(app identityProviders.AppDetail) (string, error) {
	appResponse, err := o.httpClient.Get(app.LinkURL)
	if err != nil {
		return "", err
	}

	return saml.ParseHTML(appResponse.Body)
}

func (o *oktaClient) ListApplications(userID string) ([]identityProviders.AppDetail, error) {
	rel, _ := url.Parse(fmt.Sprintf("users/%s/appLinks", userID))
	url := o.baseURL.ResolveReference(rel)

	listApplicationRequest, err := http.NewRequest("GET", url.String(), nil)
	if err != nil {
		return nil, err
	}

	listApplicationsResponse, err := o.httpClient.Do(listApplicationRequest)
	if err != nil {
		return nil, err
	}

	var oktaApplications []oktaAppLink
	b, err := ioutil.ReadAll(listApplicationsResponse.Body)
	if err := json.Unmarshal(b, &oktaApplications); err != nil {
		return nil, err
	}

	var apps = make([]identityProviders.AppDetail, len(oktaApplications))
	for i, oktaApp := range oktaApplications {
		apps[i] = identityProviders.AppDetail{
			ID:            oktaApp.ID,
			Label:         oktaApp.Label,
			LinkURL:       oktaApp.LinkURL,
			AppName:       oktaApp.AppName,
			AppInstanceID: oktaApp.AppInstanceID,
		}
	}

	return apps, nil
}

func (o *oktaClient) cacheSession(username, sessionID, expiresAt string) error {
	session := oktaSessionCache{
		Username:  username,
		SessionID: sessionID,
		ExpiresAt: expiresAt,
	}

	existingSessions, err := o.sessionCache.Sessions()
	if err != nil {
		return err
	}

	existingSessions = append(existingSessions, session)
	return o.sessionCache.Save(existingSessions)
}

func (o *oktaClient) setSessionID(id string) {
	cookies := o.httpClient.Jar.Cookies(o.baseURL)
	cookie := &http.Cookie{
		Name:     "sid",
		Value:    id,
		Path:     "/",
		Domain:   o.baseURL.Host,
		Secure:   true,
		HttpOnly: true,
	}
	cookies = append(cookies, cookie)
	o.httpClient.Jar.SetCookies(o.baseURL, cookies)
}

func (o *oktaClient) startSession(sessionToken string) (*oktaSessionResponse, error) {
	rel, err := url.Parse("sessions")
	if err != nil {
		return nil, err
	}

	url := o.baseURL.ResolveReference(rel)
	if err != nil {
		return nil, err
	}

	sessionsRequest := oktaSessionsRequest{
		SessionToken: sessionToken,
	}

	b, err := json.Marshal(sessionsRequest)
	if err != nil {
		return nil, err
	}

	response, err := o.httpClient.Post(url.String(), applicationJSON, bytes.NewReader(b))
	if err != nil {
		return nil, err
	}

	sessionResponse := &oktaSessionResponse{}
	b, err = ioutil.ReadAll(response.Body)
	if err := json.Unmarshal(b, sessionResponse); err != nil {
		return nil, err
	}

	return sessionResponse, nil
}
