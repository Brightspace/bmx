using System.Collections.Generic;
using System.Linq;
using Bmx.Core.State;
using Bmx.Service.Aws.Models;

namespace Bmx.Service.Aws.State {
	public class AwsRoleState : IRoleState {
		public AwsRoleState( List<AwsRole> awsRoles, string samlString ) {
			AwsRoles = awsRoles;
			SamlString = samlString;
		}

		public string[] Roles => AwsRoles.Select( role => role.RoleName ).ToArray();
		internal List<AwsRole> AwsRoles { get; }
		internal string SamlString { get; }
	}
}
