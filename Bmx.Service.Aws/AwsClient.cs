﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Bmx.Core;
using Bmx.Service.Aws.Models;
using Bmx.Service.Aws.State;

namespace Bmx.Service.Aws {
	public class AwsClient : ICloudProvider<AwsRoleState> {
		private readonly IAmazonSecurityTokenService _stsClient;

		public AwsClient( IAmazonSecurityTokenService stsClient ) {
			_stsClient = stsClient;
		}

		private XmlDocument ParseSamlToken( string encodedSaml ) {
			var samlStatements = encodedSaml.Split( ";" );
			// Process the B64 Encoded SAML string to get valid XML doc
			var samlString = new StringBuilder();
			foreach( var inputValueString in samlStatements ) {
				samlString.Append( HttpUtility.HtmlDecode( inputValueString ) );
			}

			var samlData = Convert.FromBase64String( samlString.ToString() );

			var samlToken = new XmlDocument();
			samlToken.LoadXml( Encoding.UTF8.GetString( samlData ) );
			return samlToken;
		}

		public AwsRoleState GetRoles( string encodedSaml ) {
			var samlToken = ParseSamlToken( encodedSaml );
			var roleNodes = samlToken.SelectNodes( "//*[@Name=\"https://aws.amazon.com/SAML/Attributes/Role\"]/*" );

			var roles = new List<AwsRole>();

			foreach( XmlElement roleNode in roleNodes ) {
				// SAML has value: <principal-arn>, <role-arn>
				// The last part of the role-arn is a human readable name
				var nodeContents = roleNode.InnerText.Split( "," );

				roles.Add( new AwsRole {
					PrincipalArn = nodeContents[0],
					RoleArn = nodeContents[1],
					RoleName = nodeContents[1].Split( "/" )[1]
				} );
			}

			return new AwsRoleState( roles, encodedSaml );
		}

		public async Task<Dictionary<string, string>> GetTokens( AwsRoleState state, int selectedRoleIndex ) {
			var role = state.AwsRoles[selectedRoleIndex];

			// Generate access keys valid for 1 hour (default)
			var authResp = await _stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest() {
				PrincipalArn = role.PrincipalArn, RoleArn = role.RoleArn, SAMLAssertion = state.SamlString
			} );

			return new Dictionary<string, string> {
				{"AWS_SESSION_TOKEN", authResp.Credentials.SessionToken},
				{"AWS_ACCESS_KEY_ID", authResp.Credentials.AccessKeyId},
				{"AWS_SECRET_ACCESS_KEY", authResp.Credentials.SecretAccessKey}
			};
		}
	}
}
