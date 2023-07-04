using System.Net;
using System.Xml;
using D2L.Bmx.Aws;
using HtmlAgilityPack;

namespace D2L.Bmx;

internal static partial class HtmlXmlHelper {
	public static string GetSamlResponseFromLoginPage( string html ) {
		var htmlDoc = new HtmlDocument();
		htmlDoc.LoadHtml( html );

		if(
			// `SelectSingleNode` can return null even though its signature doesn't say so
			htmlDoc.DocumentNode.SelectSingleNode( "//input[@name='SAMLResponse']" ) is HtmlNode inputNode
			&& inputNode.GetAttributeValue( "value", /* default */ null ) is string samlResponse
		) {
			return WebUtility.HtmlDecode( samlResponse );
		}
		throw new BmxException( "Error parsing AWS login page" );
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
}
