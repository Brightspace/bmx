namespace D2L.Bmx.GitHub;

internal record GitHubRelease(
	string TagName,
	List<GitHubAsset> Assets
) {
	public Version? Version =>
		Version.TryParse( TagName.TrimStart( 'v' ), out var version )
		? version
		: null;
}

internal record GitHubAsset(
	string Name,
	string BrowserDownloadUrl
);
