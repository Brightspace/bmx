package cmd

import (
	"fmt"
	"os"
	"strings"

	"github.com/Brightspace/bmx/cli/console"
	"github.com/Brightspace/bmx/identityProviders"
)

const (
	passwordPrompt = "Password: "
)

func authenticate(reader console.Reader, username string, noMask bool, filter, account string, identity identityProviders.IdentityProvider) (string, error) {
	var userID string
	var password string
	var err error
	ok := false

	userID, ok, err = identity.AuthenticateFromCache(username)
	if !ok || err != nil {
		password, err = getPassword(reader, noMask)
		if err != nil {
			return "", err
		}

		userID, err = identity.Authenticate(username, password)
		if err != nil {
			if err.Error() == "MFA_REQUIRED" {
				fmt.Fprintln(os.Stderr, "MFA Required")

				mfaFactors, err := identity.GetMFAFactors()
				if err != nil {
					return "", err
				}

				for idx, factor := range mfaFactors {
					fmt.Fprintf(os.Stderr, "%d - %s\n", idx+1, factor.Factor)
				}

				var mfaIdx int
				if mfaIdx, err = reader.ReadInt("Select an available MFA option: "); err != nil {
					return "", err
				}

				if mfaIdx < 1 || mfaIdx > len(mfaFactors) {
					return "", fmt.Errorf("invalid selection")
				}

				mfaURL := mfaFactors[mfaIdx-1].URL

				var mfaCode string
				if mfaCode, err = reader.ReadLine("Code: "); err != nil {
					return "", err
				}

				userID, err = identity.CompleteMFA(username, mfaURL, mfaCode)
			} else {
				return "", err
			}
		}
	}

	applications, err := identity.ListApplications(userID)
	if err != nil {
		return "", err
	}

	var filteredApps = make([]identityProviders.AppDetail, 0)
	count := 1

	app, found := findApp(account, applications)
	if !found {
		// select an account
		fmt.Fprintln(os.Stderr, "Available accounts:")
		for _, a := range applications {
			if a.AppName == filter || filter == "" {
				filteredApps = append(filteredApps, a)
				os.Stderr.WriteString(fmt.Sprintf("[%d] %s\n", count, a.Label))
				count++
			}
		}

		var accountID int
		if accountID, err = reader.ReadInt("Select an account: "); err != nil {
			return "", err
		}

		if accountID < 1 || accountID > len(filteredApps) {
			return "", fmt.Errorf("invalid selection")
		}

		app = filteredApps[accountID-1]
	}

	return identity.GetSaml(app)
}

func findApp(account string, apps []identityProviders.AppDetail) (identityProviders.AppDetail, bool) {
	account = strings.ToLower(account)
	for _, app := range apps {
		if strings.ToLower(app.Label) == account {
			return app, true
		}
	}

	return identityProviders.AppDetail{}, false
}

func getPassword(reader console.Reader, noMask bool) (string, error) {
	var pass string
	if noMask {
		var err error
		pass, err = reader.ReadLine(passwordPrompt)
		if err != nil {
			return "", err
		}

	} else {
		var err error
		pass, err = reader.ReadPassword(passwordPrompt)
		if err != nil {
			return "", err
		}

		fmt.Fprintln(os.Stderr)
	}

	return pass, nil
}
