package bmx

import (
	"fmt"
	"strings"

	"github.com/Brightspace/bmx/identityProviders"
	"github.com/Brightspace/bmx/io"
)

const (
	invalidSelection = "invalid selection"
	mfaRequired      = "MFA_REQUIRED"
	passwordPrompt   = "Password: "
	userPrompt       = "Username: "
)

func authenticate(rw io.ReadWriter, username string, noMask bool, filter, account string, identity identityProviders.IdentityProvider) (string, error) {
	var userID string
	var password string
	var err error
	ok := false

	username = strings.TrimSpace(username)
	if username == "" {
		username, err = rw.ReadLine(userPrompt)
		if err != nil {
			return "", err
		}
	}

	userID, ok, err = identity.AuthenticateFromCache(username)
	if !ok || err != nil {
		password, err = getPassword(rw, noMask)
		if err != nil {
			return "", err
		}

		userID, err = identity.Authenticate(username, password)
		if err != nil {
			if err.Error() == mfaRequired {
				rw.Writeln("MFA Required")

				mfaFactors, err := identity.GetMFAFactors()
				if err != nil {
					return "", err
				}

				for idx, factor := range mfaFactors {
					rw.Writeln(fmt.Sprintf("%d - %s\n", idx+1, factor.Factor))
				}

				var mfaIdx int
				if mfaIdx, err = rw.ReadInt("Select an available MFA option: "); err != nil {
					return "", err
				}

				if mfaIdx < 1 || mfaIdx > len(mfaFactors) {
					return "", fmt.Errorf(invalidSelection)
				}

				mfaURL := mfaFactors[mfaIdx-1].URL

				var mfaCode string
				if mfaCode, err = rw.ReadLine("Code: "); err != nil {
					return "", err
				}

				userID, err = identity.CompleteMFA(username, mfaURL, mfaCode)
				if err != nil {
					return "", err
				}
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
		rw.Writeln("Available accounts:")
		for _, a := range applications {
			if a.AppName == filter || filter == "" {
				filteredApps = append(filteredApps, a)
				rw.Writeln(fmt.Sprintf("[%d] %s", count, a.Label))
				count++
			}
		}

		var accountID int
		if accountID, err = rw.ReadInt("Select an account: "); err != nil {
			return "", err
		}

		if accountID < 1 || accountID > len(filteredApps) {
			return "", fmt.Errorf(invalidSelection)
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

func getPassword(rw io.ReadWriter, noMask bool) (string, error) {
	var pass string
	if noMask {
		var err error
		pass, err = rw.ReadLine(passwordPrompt)
		if err != nil {
			return "", err
		}

	} else {
		var err error
		pass, err = rw.ReadPassword(passwordPrompt)
		if err != nil {
			return "", err
		}

		rw.Writeln("")
	}

	return pass, nil
}
