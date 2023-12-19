using System.Text.Json;
using System.Text.Json.Serialization;

namespace D2L.Bmx;
internal record GithubRelease {
	[JsonPropertyName( "tag_name" )]
	public string? TagName { get; init; }

	[JsonPropertyName( "assets" )]
	public List<GithubAsset>? Assets { get; init; }
}

internal record GithubAsset {
	[JsonPropertyName( "name" )]
	public string? Name { get; init; }

	[JsonPropertyName( "browser_download_url" )]
	public string? BrowserDownloadUrl { get; init; }
}

internal static class GithubUtilities {
	public static string GetReleaseVersion( GithubRelease? releaseData ) {
		string version = releaseData?.TagName?.TrimStart( 'v' ) ?? string.Empty;
		return version;
	}

	public static async Task<GithubRelease?> GetLatestReleaseDataAsync() {
		using var httpClient = new HttpClient();
		httpClient.BaseAddress = new Uri( "https://api.github.com" );
		httpClient.Timeout = TimeSpan.FromSeconds( 2 );
		httpClient.DefaultRequestHeaders.Add( "User-Agent", "BMX" );
		var response = await httpClient.GetAsync( "repos/Brightspace/bmx/releases/latest" );
		response.EnsureSuccessStatusCode();

		await using var responseStream = await response.Content.ReadAsStreamAsync();
		var releaseData = await JsonSerializer.DeserializeAsync(
			responseStream,
			SourceGenerationContext.Default.GithubRelease
		);
		return releaseData;
	}
}
