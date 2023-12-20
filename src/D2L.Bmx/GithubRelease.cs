using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace D2L.Bmx;
internal record GithubRelease {
	[JsonPropertyName( "tag_name" )]
	public string? TagName { get; init; }

	[JsonPropertyName( "assets" )]
	public List<GithubAsset>? Assets { get; init; }

	public Version? GetReleaseVersion() {
		string? version = TagName?.TrimStart( 'v' ) ?? null;
		if( version is null ) {
			return null;
		}
		return new Version( version );
	}

	public static async Task<GithubRelease?> GetLatestReleaseDataAsync() {
		using var httpClient = new HttpClient();
		httpClient.BaseAddress = new Uri( "https://api.github.com" );
		httpClient.Timeout = TimeSpan.FromSeconds( 2 );
		httpClient.DefaultRequestHeaders.Add( "User-Agent", "BMX" );
		return await httpClient.GetFromJsonAsync(
			"repos/Brightspace/bmx/releases/latest",
			SourceGenerationContext.Default.GithubRelease );
	}
}

internal record GithubAsset {
	[JsonPropertyName( "name" )]
	public string? Name { get; init; }

	[JsonPropertyName( "browser_download_url" )]
	public string? BrowserDownloadUrl { get; init; }
}
