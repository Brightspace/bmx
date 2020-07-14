namespace Bmx.CommandLine {
	public interface IBmxConfig {
		public string Org { get; }
		public string User { get; }
		public string Account { get; }
		public string Role { get; }
		public string Profile { get; }
	}
}
