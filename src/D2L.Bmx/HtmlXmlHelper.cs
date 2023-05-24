using System.Text.RegularExpressions;
using System.Xml;
using D2L.Bmx.Aws;

namespace D2L.Bmx;

internal static partial class HtmlXmlHelper {
	public static string GetSamlResponseFromLoginPage( string html ) {
		// TODO: use proper HTML parsing instead of regex
		var inputXml = new XmlDocument();
		inputXml.LoadXml( SamlResponseInputRegex().Match( html ).Value );

		return inputXml.SelectSingleNode( "//@value" )?.InnerText
			?? throw new BmxException( "Error parsing AWS login page" );
	}

	public static AwsRole[] GetRolesFromSamlResponse( string samlResponse ) {
		byte[] samlXmlBytes = Convert.FromBase64String( samlResponse );
		var samlXml = new XmlDocument();
		using( var ms = new MemoryStream( samlXmlBytes ) ) {
			samlXml.Load( ms );
		}

		var nsManager = new XmlNamespaceManager( samlXml.NameTable );
		nsManager.AddNamespace( "s2", "urn:oasis:names:tc:SAML:2.0:assertion" );
		XmlNodeList roleNodes = samlXml.SelectNodes(
			"""//s2:Attribute[@Name="https://aws.amazon.com/SAML/Attributes/Role"]/s2:AttributeValue""",
			nsManager
		) ?? throw new BmxException( "Failed to retrieve roles" );

		return roleNodes
			.Cast<XmlElement>()
			.Select( node =>
				node.InnerText.Split( ',' ) is [string principalArn, string roleArn]
				? new AwsRole(
					RoleName: roleArn.Split( "/" )[^1],
					PrincipalArn: principalArn,
					RoleArn: roleArn
				)
				: throw new BmxException( "Failed to retrieve roles" ) )
			.ToArray();
	}

	[GeneratedRegex( """<input name="SAMLResponse" type="hidden" value=".*?"/>""" )]
	private static partial Regex SamlResponseInputRegex();
}
