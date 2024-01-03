using System.Net.Http.Json;

namespace D2L.Bmx.GitHub;

internal interface IGitHubClient {
	Task<GitHubRelease> GetLatestBmxReleaseAsync();
	Task DownloadAssetAsync( GitHubAsset asset, string downloadPath );
}

internal class GitHubClient : IGitHubClient {
	private readonly HttpClient _httpClient = new() {
		BaseAddress = new Uri( "https://api.github.com" ),
		Timeout = TimeSpan.FromSeconds( 2 ),
		DefaultRequestHeaders = {
			{ "User-Agent", "BMX" },
			{ "Accept", "application/vnd.github+json" },
		},
	};

	public async Task<GitHubRelease> GetLatestBmxReleaseAsync() {
		return await _httpClient.GetFromJsonAsync(
			"repos/Brightspace/bmx/releases/latest",
			JsonSnakeCaseContext.Default.GitHubRelease
		) ??
		// this should never happen as per GitHub docs
		throw new BmxException( "Failed to find the latest BMX release" );
	}

	public async Task DownloadAssetAsync( GitHubAsset asset, string downloadPath ) {
		var response = await _httpClient.GetAsync( asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead );
		response.EnsureSuccessStatusCode();

		var responseStream = await response.Content.ReadAsStreamAsync();
		using var fileStream = File.OpenWrite( downloadPath );
		responseStream.CopyTo( fileStream );
		fileStream.Flush();
	}
}
