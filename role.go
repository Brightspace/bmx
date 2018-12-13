package bmx

import (
	"fmt"
	"strings"

	"github.com/Brightspace/bmx/io"
	"github.com/Brightspace/bmx/serviceProviders"

	"github.com/aws/aws-sdk-go/service/sts"
)

func selectRole(rw io.ReadWriter, desiredRole, saml string, service serviceProviders.ServiceProvider) (*sts.Credentials, error) {
	roles, err := service.ListRoles(saml)
	if err != nil {
		return nil, err
	}

	if len(roles) == 0 {
		return nil, fmt.Errorf("no roles available")
	}

	var role *serviceProviders.Role
	if desiredRole != "" {
		desiredRole = strings.ToLower(desiredRole)
		for _, r := range roles {
			if strings.ToLower(r.Name) == desiredRole {
				role = &r
				break
			}
		}

		if role == nil {
			rw.Writeln("Desired role not available")
		}
	}

	if role == nil {
		if len(roles) == 1 {
			role = &roles[0]
		} else {
			for idx, r := range roles {
				rw.Writeln(fmt.Sprintf("[%d] %s", idx+1, r.Name))
			}

			roleSelection, err := rw.ReadInt("Select a role: ")
			if err != nil {
				return nil, err
			}

			if roleSelection < 1 || roleSelection > len(roles) {
				return nil, fmt.Errorf(invalidSelection)
			}

			role = &roles[roleSelection-1]
		}
	}

	creds, err := service.GetCredentials(*role, saml)
	if err != nil {
		return nil, err
	}

	return creds, nil
}
