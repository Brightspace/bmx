using System;
using System.Xml;
using Bmx.Core;

namespace Bmx.Service.Aws {
	public class AwsClient : ICloudProvider {
		private XmlDocument _samlToken;

		public void SetSamlToken( XmlDocument samlToken ) {
			_samlToken = samlToken;
		}

		public string[] GetRoles() {
			throw new NotImplementedException();
		}
	}
}
