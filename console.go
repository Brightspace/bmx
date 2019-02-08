package bmx

import (
	"fmt"
	"log"
	"os"
	"strings"

	"github.com/Brightspace/bmx/console"
	"github.com/Brightspace/bmx/saml/identityProviders"
	"github.com/Brightspace/bmx/saml/identityProviders/okta"
	"github.com/Brightspace/bmx/saml/serviceProviders"
)

const (
	usernamePrompt = "Okta Username: "
	passwordPrompt = "Okta Password: "
)

var (
	ConsoleReader console.ConsoleReader
)

func init() {
	ConsoleReader = console.DefaultConsoleReader{}
}

func getUserIfEmpty(usernameFlag string) string {
	var username string
	if len(usernameFlag) == 0 {
		var err error
		username, err = ConsoleReader.ReadLine(usernamePrompt)
		if err != nil {
			log.Fatal(err)
		}
	} else {
		username = usernameFlag
	}
	return username
}

func getPassword(noMask bool) string {
	var pass string
	if noMask {
		var err error
		pass, err = ConsoleReader.ReadLine(passwordPrompt)
		if err != nil {
			log.Fatal(err)
		}
	} else {
		var err error
		pass, err = ConsoleReader.ReadPassword(passwordPrompt)
		if err != nil {
			log.Fatal(err)
		}
		fmt.Fprintln(os.Stderr)
	}
	return pass
}

func authenticate(user serviceProviders.UserInfo, oktaClient identityProviders.IdentityProvider) (string, error) {
	var userID string
	var ok bool
	userID, ok = oktaClient.AuthenticateFromCache(user.User, user.Org)
	if !ok {
		user.Password = getPassword(user.NoMask)
		var err error
		userID, err = oktaClient.Authenticate(user.User, user.Password, user.Org)
		if err != nil {
			log.Fatal(err)
		}
	}

	oktaApplications, err := oktaClient.ListApplications(userID)
	if err != nil {
		log.Fatal(err)
	}

	app, found := findApp(user.Account, oktaApplications)
	if !found {
		// select an account
		fmt.Fprintln(os.Stderr, "Available accounts:")
		for idx, a := range oktaApplications {
			if a.AppName == "amazon_aws" {
				os.Stderr.WriteString(fmt.Sprintf("[%d] %s\n", idx, a.Label))
			}
		}
		var accountId int
		if accountId, err = ConsoleReader.ReadInt("Select an account: "); err != nil {
			log.Fatal(err)
		}
		app = &oktaApplications[accountId]
	}

	return oktaClient.GetSaml(*app)
}

func findApp(app string, apps []okta.OktaAppLink) (foundApp *okta.OktaAppLink, found bool) {
	for _, v := range apps {
		if strings.ToLower(v.Label) == strings.ToLower(app) {
			return &v, true
		}
	}

	return nil, false
}
