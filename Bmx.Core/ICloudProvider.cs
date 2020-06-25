using System.Xml;

namespace Bmx.Core {
	public interface ICloudProvider {
		void SetSamlToken( XmlDocument samlToken );
		string[] GetRoles();
	}
}
