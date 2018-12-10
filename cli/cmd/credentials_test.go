package cmd

import (
	"fmt"
	"testing"
	"time"

	"github.com/Brightspace/bmx/cli/console"
	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/serviceProviders"
	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/service/sts"
)

func TestGetCredentials(t *testing.T) {
	readerMock := console.NewMockReader()
	identityMock := identityProviders.NewMockProvider()
	serviceMock := serviceProviders.NewMockProvider()

	t.Run("authenticate error", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		testErrorMessage := "test - error authenticating"
		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return nil, fmt.Errorf(testErrorMessage)
		})

		_, err := getCredentials(readerMock, identityMock, serviceMock, username, noMask, filter, account, desiredRole)
		if err == nil {
			t.Fatalf("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("select role error", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return []identityProviders.AppDetail{identityProviders.AppDetail{Label: account}}, nil
		})

		identityMock.MockGetSaml(func(identityProviders.AppDetail) (string, error) {
			return dummySaml, nil
		})

		testErrorMessage := "test - error selecting role"
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return nil, fmt.Errorf(testErrorMessage)
		})

		_, err := getCredentials(readerMock, identityMock, serviceMock, username, noMask, filter, account, desiredRole)
		if err == nil {
			t.Fatalf("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("success", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return []identityProviders.AppDetail{identityProviders.AppDetail{Label: account}}, nil
		})

		identityMock.MockGetSaml(func(identityProviders.AppDetail) (string, error) {
			return dummySaml, nil
		})

		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return []serviceProviders.Role{
				serviceProviders.Role{Name: desiredRole},
			}, nil
		})

		creds := &sts.Credentials{
			AccessKeyId:     aws.String("access_key_id"),
			SecretAccessKey: aws.String("secrest_access_key"),
			SessionToken:    aws.String("session_token"),
			Expiration:      aws.Time(time.Now().Add(time.Hour * 8)),
		}

		serviceMock.MockGetCredentials(func(serviceProviders.Role, string) (*sts.Credentials, error) {
			return creds, nil
		})

		returnedCredentials, err := getCredentials(readerMock, identityMock, serviceMock, username, noMask, filter, account, desiredRole)
		if err != nil {
			t.Fatal(err)
		}

		if returnedCredentials != creds {
			t.Errorf("incorrect credentials, expected: %v, got: %v", creds, returnedCredentials)
		}
	})
}
