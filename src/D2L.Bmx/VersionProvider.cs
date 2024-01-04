using System.Reflection;

namespace D2L.Bmx;

internal interface IVersionProvider {
	Version? GetAssemblyVersion();
	string? GetInformationalVersion();
}

internal class VersionProvider : IVersionProvider {
	private readonly Assembly _currentAssembly = Assembly.GetExecutingAssembly();

	public Version? GetAssemblyVersion() {
		return _currentAssembly.GetName().Version;
	}

	public string? GetInformationalVersion() {
		return _currentAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
	}
}
