using System.Text.Json.Serialization;

namespace D2L.Bmx;
internal record GithubRelease {
	[JsonPropertyName( "tag_name" )]
	public string? TagName { get; set; }

	[JsonPropertyName( "assets" )]
	public List<Assets>? Assets { get; set; }
}

internal record Assets {
	[JsonPropertyName( "name" )]
	public string? Name { get; set; }

	[JsonPropertyName( "browser_download_url" )]
	public string? BrowserDownloadUrl { get; set; }
}
