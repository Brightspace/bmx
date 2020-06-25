using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bmx.Core;
using Bmx.Service.Aws.Models;

namespace Bmx.Service.Aws {
	public class AwsClient : ICloudProvider {
		private string _samlString;
		private XmlDocument _samlToken;
		private List<AwsRole> _awsRoles;

		public void SetSamlToken( XmlDocument samlToken ) {
			_samlToken = samlToken;
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
	}
}
