using System.Net;
using System.Text;
using System.Xml;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using D2L.Bmx.Aws.Models;

namespace D2L.Bmx.Aws;

internal interface IAwsClient {
	AwsRoleState GetRoles( string encodedSaml );
	Task<AwsCredentials> GetTokensAsync( AwsRoleState state, string selectedRole, int durationInMinutes );
}

internal class AwsClient : IAwsClient {
	private readonly IAmazonSecurityTokenService _stsClient;

	public AwsClient( IAmazonSecurityTokenService stsClient ) {
		_stsClient = stsClient;
	}

	AwsRoleState IAwsClient.GetRoles( string encodedSaml ) {
		var samlToken = ParseSamlToken( encodedSaml );
		var roleNodes = samlToken.SelectNodes( "//*[@Name=\"https://aws.amazon.com/SAML/Attributes/Role\"]/*" );

		var roles = new List<AwsRole>();

		if( roleNodes is not null ) {
			foreach( XmlElement roleNode in roleNodes ) {
				// SAML has value: <principal-arn>, <role-arn>
				// The last part of the role-arn is a human readable name
				var nodeContents = roleNode.InnerText.Split( "," );

				roles.Add( new AwsRole(
					RoleName: nodeContents[1].Split( "/" )[1],
					PrincipalArn: nodeContents[0],
					RoleArn: nodeContents[1]
					) );
			}
		} else {
			throw new BmxException( "Failed to retrieve roles" );
		}
		return new AwsRoleState( roles, encodedSaml );
	}

	async Task<AwsCredentials> IAwsClient.GetTokensAsync(
		AwsRoleState state,
		string selectedRole,
		int durationInMinutes ) {
		var role = state.AwsRoles
			.Find( role => string.Equals( role.RoleName, selectedRole, StringComparison.OrdinalIgnoreCase ) );

		if( role is not null ) {

			// Generate access keys valid for 1 hour (default)
			var authResp = await _stsClient.AssumeRoleWithSAMLAsync( new AssumeRoleWithSAMLRequest() {
				PrincipalArn = role.PrincipalArn,
				RoleArn = role.RoleArn,
				SAMLAssertion = state.SamlString,
			} );

			return new AwsCredentials(
				SessionToken: authResp.Credentials.SessionToken,
				AccessKeyId: authResp.Credentials.AccessKeyId,
				SecretAccessKey: authResp.Credentials.SecretAccessKey,
				Expiration: authResp.Credentials.Expiration
			);
		}
		throw new BmxException( "Invalid role selection" );
	}

	private XmlDocument ParseSamlToken( string encodedSaml ) {
		var samlStatements = encodedSaml.Split( ";" );

		// Process the B64 Encoded SAML string to get valid XML doc
		var samlString = new StringBuilder();
		foreach( var inputValueString in samlStatements ) {
			samlString.Append( WebUtility.HtmlDecode( inputValueString ) );
		}

		var samlData = Convert.FromBase64String( samlString.ToString() );

		var samlToken = new XmlDocument();
		samlToken.LoadXml( Encoding.UTF8.GetString( samlData ) );
		return samlToken;
	}
}
