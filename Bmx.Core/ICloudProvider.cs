namespace Bmx.Core {
	public interface ICloudProvider {
		void SetSamlToken( string encodedSaml );
		string[] GetRoles();
	}
}
