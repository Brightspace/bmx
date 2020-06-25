namespace Bmx.Idp.Okta.Models {
	public struct OktaMfaFactor {
		public string Id { get; set; }
		public string FactorType { get; set; }
		public string Provider { get; set; }
		public string VendorName { get; set; }
	}
}
