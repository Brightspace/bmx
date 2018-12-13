package bmx

import (
	"fmt"
	"testing"
	"time"

	"github.com/aws/aws-sdk-go/aws"
	"github.com/aws/aws-sdk-go/service/sts"

	"github.com/Brightspace/bmx/io"
	"github.com/Brightspace/bmx/serviceProviders"
)

const (
	desiredRole = "desiredRole"
	saml        = "saml"
)

func TestSelectRole(t *testing.T) {
	rwMock := io.NewMockReadWriter()
	serviceMock := serviceProviders.NewMockProvider()

	t.Run("list roles error", func(t *testing.T) {
		testErrorMessage := "test - error listing roles"
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return nil, fmt.Errorf(testErrorMessage)
		})

		_, err := selectRole(rwMock, desiredRole, saml, serviceMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("no roles available", func(t *testing.T) {
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return []serviceProviders.Role{}, nil
		})

		_, err := selectRole(rwMock, desiredRole, saml, serviceMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		expectedMessage := "no roles available"
		if err.Error() != expectedMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", expectedMessage, err)
		}
	})

	t.Run("error getting credentials", func(t *testing.T) {
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return []serviceProviders.Role{
				serviceProviders.Role{Name: "owner"},
			}, nil
		})

		testErrorMessage := "test - error getting credentials"
		serviceMock.MockGetCredentials(func(serviceProviders.Role, string) (*sts.Credentials, error) {
			return nil, fmt.Errorf(testErrorMessage)
		})

		_, err := selectRole(rwMock, desiredRole, saml, serviceMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("error reading input", func(t *testing.T) {
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return []serviceProviders.Role{
				serviceProviders.Role{Name: "owner"},
				serviceProviders.Role{Name: "user"},
			}, nil
		})

		testErrorMessage := "test - error reading input"
		rwMock.MockReadInt(func(string) (int, error) {
			return 0, fmt.Errorf(testErrorMessage)
		})

		_, err := selectRole(rwMock, desiredRole, saml, serviceMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("invalid role selection", func(t *testing.T) {
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return []serviceProviders.Role{
				serviceProviders.Role{Name: "owner"},
				serviceProviders.Role{Name: "user"},
			}, nil
		})

		selections := []int{0, 3}
		for _, selection := range selections {
			rwMock.MockReadInt(func(string) (int, error) {
				return selection, nil
			})

			_, err := selectRole(rwMock, desiredRole, saml, serviceMock)
			if err == nil {
				t.Error("expected error, got none")
			}

			if err.Error() != invalidSelection {
				t.Errorf("incorrect error message, expected: %s, got: %s", invalidSelection, err)
			}
		}
	})

	t.Run("success", func(t *testing.T) {
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

		returnedCredentials, err := selectRole(rwMock, desiredRole, saml, serviceMock)
		if err != nil {
			t.Fatal(err)
		}

		if returnedCredentials != creds {
			t.Errorf("incorrect credentials, expected: %v, got: %v", creds, returnedCredentials)
		}
	})

	t.Run("success with selection", func(t *testing.T) {
		serviceMock.MockListRoles(func(string) ([]serviceProviders.Role, error) {
			return []serviceProviders.Role{
				serviceProviders.Role{Name: "owner"},
				serviceProviders.Role{Name: "user"},
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

		rwMock.MockReadInt(func(string) (int, error) {
			return 2, nil
		})

		returnedCredentials, err := selectRole(rwMock, desiredRole, saml, serviceMock)
		if err != nil {
			t.Fatal(err)
		}

		if returnedCredentials != creds {
			t.Errorf("incorrect credentials, expected: %v, got: %v", creds, returnedCredentials)
		}
	})
}
