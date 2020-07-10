using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Amazon.SecurityToken.Model;
using Bmx.Core;
using Bmx.Service.Aws.Models;

namespace Bmx.Service.Aws {
	public class AwsClient : ICloudProvider {
		private readonly IAwsStsClient _stsClient;

		private string _samlString;
		private XmlDocument _samlToken;
		private List<AwsRole> _awsRoles;

		public AwsClient( IAwsStsClient stsClient = null ) {
			if( stsClient == null ) {
				_stsClient = new AwsStsClient();
			}
		}

		public void SetSamlToken( string encodedSaml ) {
			_samlString = encodedSaml;

			var samlStatements = _samlString.Split( ";" );
			// Process the B64 Encoded SAML string to get valid XML doc
			var samlString = new StringBuilder();
			foreach( var inputValueString in samlStatements ) {
				samlString.Append( HttpUtility.HtmlDecode( inputValueString ) );
			}

			var samlData = Convert.FromBase64String( samlString.ToString() );

			_samlToken = new XmlDocument();
			_samlToken.LoadXml( Encoding.UTF8.GetString( samlData ) );
		}

		public string[] GetRoles() {
			var roleNodes = _samlToken.SelectNodes( "//*[@Name=\"https://aws.amazon.com/SAML/Attributes/Role\"]/*" );

			_awsRoles = new List<AwsRole>();

			foreach( XmlElement roleNode in roleNodes ) {
				// SAML has value: <principal-arn>, <role-arn>
				// The last part of the role-arn is a human readable name
				var nodeContents = roleNode.InnerText.Split( "," );

				_awsRoles.Add( new AwsRole {
					PrincipalArn = nodeContents[0],
					RoleArn = nodeContents[1],
					RoleName = nodeContents[1].Split( "/" )[1]
				} );
			}

			return _awsRoles.Select( role => role.RoleName ).ToArray();
		}

		public async Task<Dictionary<string, string>> GetTokens( int selectedRoleIndex ) {
			var role = _awsRoles[selectedRoleIndex];

			// Generate access keys valid for 1 hour (default)
			var authResp = await _stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest() {
				PrincipalArn = role.PrincipalArn, RoleArn = role.RoleArn, SAMLAssertion = _samlString
			} );

			return new Dictionary<string, string> {
				{"AWS_SESSION_TOKEN", authResp.Credentials.SessionToken},
				{"AWS_ACCESS_KEY_ID", authResp.Credentials.AccessKeyId},
				{"AWS_SECRET_ACCESS_KEY", authResp.Credentials.SecretAccessKey}
			};
		}
	}
}
