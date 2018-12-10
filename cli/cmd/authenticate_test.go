package cmd

import (
	"fmt"
	"testing"

	"github.com/Brightspace/bmx/cli/console"
	"github.com/Brightspace/bmx/identityProviders"
)

const (
	username = "testuser"
	noMask   = false
	filter   = "amazon_aws"
	account  = "account"

	dummySaml = "something something saml"
)

func TestAuthenticate(t *testing.T) {
	readerMock := console.NewMockReader()
	identityMock := identityProviders.NewMockProvider()

	t.Run("authenticates from cache", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return []identityProviders.AppDetail{identityProviders.AppDetail{Label: account}}, nil
		})

		identityMock.MockGetSaml(func(identityProviders.AppDetail) (string, error) {
			return dummySaml, nil
		})

		saml, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err != nil {
			t.Fatal(err)
		}

		if saml != dummySaml {
			t.Errorf("incorrect saml response, expected: %s, got: %s", dummySaml, saml)
		}
	})

	t.Run("ListApplications error", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		testErrorMessage := "test - error listing applications"
		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return nil, fmt.Errorf(testErrorMessage)
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("error reading input", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return []identityProviders.AppDetail{
				identityProviders.AppDetail{Label: "some app"},
				identityProviders.AppDetail{Label: "another app"},
			}, nil
		})

		testErrorMessage := "test - error reading int"
		readerMock.MockReadInt(func(string) (int, error) {
			return 0, fmt.Errorf(testErrorMessage)
		})

		_, err := authenticate(readerMock, username, noMask, "", account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("invalid app selection", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return []identityProviders.AppDetail{
				identityProviders.AppDetail{Label: "some app"},
				identityProviders.AppDetail{Label: "another app"},
			}, nil
		})

		selections := []int{0, 3}
		for _, selection := range selections {
			readerMock.MockReadInt(func(string) (int, error) {
				return selection, nil
			})

			_, err := authenticate(readerMock, username, noMask, "", account, identityMock)
			if err == nil {
				t.Error("expected error, got none")
			}

			if err.Error() != invalidSelection {
				t.Errorf("incorrect error message, expected: %s, got: %s", invalidSelection, err)
			}
		}
	})

	t.Run("app selected", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", true, nil
		})

		expectedAppLabel := "another app"
		identityMock.MockListApplications(func(string) ([]identityProviders.AppDetail, error) {
			return []identityProviders.AppDetail{
				identityProviders.AppDetail{Label: "some app"},
				identityProviders.AppDetail{Label: expectedAppLabel},
			}, nil
		})

		identityMock.MockGetSaml(func(app identityProviders.AppDetail) (string, error) {
			if app.Label != expectedAppLabel {
				t.Fatalf("incorrect app label, expected: %s, got: %s", expectedAppLabel, app.Label)
			}

			return dummySaml, nil
		})

		readerMock.MockReadInt(func(string) (int, error) {
			return 2, nil
		})

		saml, err := authenticate(readerMock, username, noMask, "", account, identityMock)
		if err != nil {
			t.Fatal(err)
		}

		if saml != dummySaml {
			t.Errorf("incorrect saml response, expected: %s, got: %s", dummySaml, saml)
		}
	})

	t.Run("get password error", func(t *testing.T) {
		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		testErrorMessage := "test - error reading password"
		readerMock.MockReadPassword(func(string) (string, error) {
			return "", fmt.Errorf(testErrorMessage)
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("authenticate error", func(t *testing.T) {
		testErrorMessage := "test - error authenticating"
		identityMock.MockAuthenticate(func(string, string) (string, error) {
			return "", fmt.Errorf(testErrorMessage)
		})

		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		readerMock.MockReadPassword(func(string) (string, error) {
			return "", nil
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("error getting MFA", func(t *testing.T) {
		identityMock.MockAuthenticate(func(string, string) (string, error) {
			return "", fmt.Errorf(mfaRequired)
		})

		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		testErrorMessage := "test - error getting MFA factors"
		identityMock.MockGetMFAFactors(func() ([]identityProviders.MFAFactor, error) {
			return nil, fmt.Errorf(testErrorMessage)
		})

		readerMock.MockReadPassword(func(string) (string, error) {
			return "", nil
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("error reading MFA selection", func(t *testing.T) {
		identityMock.MockAuthenticate(func(string, string) (string, error) {
			return "", fmt.Errorf(mfaRequired)
		})

		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		identityMock.MockGetMFAFactors(func() ([]identityProviders.MFAFactor, error) {
			return []identityProviders.MFAFactor{
				identityProviders.MFAFactor{},
			}, nil
		})

		testErrorMessage := "test - error reading MFA selection"
		readerMock.MockReadInt(func(string) (int, error) {
			return 0, fmt.Errorf(testErrorMessage)
		})

		readerMock.MockReadPassword(func(string) (string, error) {
			return "", nil
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("invalid MFA selection", func(t *testing.T) {
		identityMock.MockAuthenticate(func(string, string) (string, error) {
			return "", fmt.Errorf(mfaRequired)
		})

		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		identityMock.MockGetMFAFactors(func() ([]identityProviders.MFAFactor, error) {
			return []identityProviders.MFAFactor{
				identityProviders.MFAFactor{},
			}, nil
		})

		readerMock.MockReadPassword(func(string) (string, error) {
			return "", nil
		})

		selections := []int{0, 3}
		for _, selection := range selections {
			readerMock.MockReadInt(func(string) (int, error) {
				return selection, nil
			})

			_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
			if err == nil {
				t.Error("expected error, got none")
			}

			if err.Error() != invalidSelection {
				t.Errorf("incorrect error message, expected: %s, got: %s", invalidSelection, err)
			}
		}
	})

	t.Run("error reading MFA code", func(t *testing.T) {
		identityMock.MockAuthenticate(func(string, string) (string, error) {
			return "", fmt.Errorf(mfaRequired)
		})

		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		identityMock.MockGetMFAFactors(func() ([]identityProviders.MFAFactor, error) {
			return []identityProviders.MFAFactor{
				identityProviders.MFAFactor{},
			}, nil
		})

		readerMock.MockReadInt(func(string) (int, error) {
			return 1, nil
		})

		testErrorMessage := "test - error reading MFA code"
		readerMock.MockReadLine(func(string) (string, error) {
			return "", fmt.Errorf(testErrorMessage)
		})

		readerMock.MockReadPassword(func(string) (string, error) {
			return "", nil
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("error completing MFA", func(t *testing.T) {
		identityMock.MockAuthenticate(func(string, string) (string, error) {
			return "", fmt.Errorf(mfaRequired)
		})

		identityMock.MockAuthenticateFromCache(func(string) (string, bool, error) {
			return "", false, nil
		})

		testErrorMessage := "test - error completing MFA"
		identityMock.MockCompleteMFA(func(string, string, string) (string, error) {
			return "", fmt.Errorf(testErrorMessage)
		})

		identityMock.MockGetMFAFactors(func() ([]identityProviders.MFAFactor, error) {
			return []identityProviders.MFAFactor{
				identityProviders.MFAFactor{},
			}, nil
		})

		readerMock.MockReadInt(func(string) (int, error) {
			return 1, nil
		})

		readerMock.MockReadLine(func(string) (string, error) {
			return "", nil
		})

		readerMock.MockReadPassword(func(string) (string, error) {
			return "", nil
		})

		_, err := authenticate(readerMock, username, noMask, filter, account, identityMock)
		if err == nil {
			t.Error("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})
}

func TestFindApp(t *testing.T) {
	apps := []identityProviders.AppDetail{
		identityProviders.AppDetail{ID: "A", Label: "sOmE ApP"},
		identityProviders.AppDetail{ID: "B", Label: "aNoThEr aPp"},
	}

	t.Run("found", func(t *testing.T) {
		app, found := findApp("another APP", apps)
		if !found {
			t.Fatalf("expected found, got not found")
		}

		if app.ID != apps[1].ID {
			t.Errorf("incorrect app ID, expected: %s, got: %s", apps[1].ID, app.ID)
		}
	})

	t.Run("not found", func(t *testing.T) {
		_, found := findApp(account, apps)
		if found {
			t.Errorf("expected not found, got found")
		}
	})
}

func TestGetPassword(t *testing.T) {
	readerMock := console.NewMockReader()

	t.Run("error reading unmasked", func(t *testing.T) {
		testErrorMessage := "testing - error reading password"
		readerMock.MockReadLine(func(string) (string, error) {
			return "", fmt.Errorf(testErrorMessage)
		})

		_, err := getPassword(readerMock, true)
		if err == nil {
			t.Fatalf("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("error reading masked", func(t *testing.T) {
		testErrorMessage := "testing - error reading password"
		readerMock.MockReadPassword(func(string) (string, error) {
			return "", fmt.Errorf(testErrorMessage)
		})

		_, err := getPassword(readerMock, false)
		if err == nil {
			t.Fatalf("expected error, got none")
		}

		if err.Error() != testErrorMessage {
			t.Errorf("incorrect error message, expected: %s, got: %s", testErrorMessage, err)
		}
	})

	t.Run("success", func(t *testing.T) {
		password := "secret"
		readerMock.MockReadPassword(func(string) (string, error) {
			return password, nil
		})

		returnedPassword, err := getPassword(readerMock, false)
		if err != nil {
			t.Fatal(err)
		}

		if returnedPassword != password {
			t.Errorf("incorrect password returned, expected: %s, got: %s", password, returnedPassword)
		}
	})
}
