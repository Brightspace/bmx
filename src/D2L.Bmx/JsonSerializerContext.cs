using System.Text.Json.Serialization;
using D2L.Bmx.Aws;
using D2L.Bmx.GitHub;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	AllowOutOfOrderMetadataProperties = true
)]
[JsonSerializable( typeof( AuthenticateRequest ) )]
[JsonSerializable( typeof( IssueMfaChallengeRequest ) )]
[JsonSerializable( typeof( VerifyMfaChallengeResponseRequest ) )]
[JsonSerializable( typeof( CreateSessionRequest ) )]
[JsonSerializable( typeof( AuthenticateResponseRaw ) )]
[JsonSerializable( typeof( OktaSession ) )]
[JsonSerializable( typeof( OktaApp[] ) )]
[JsonSerializable( typeof( OktaMeResponse ) )]
[JsonSerializable( typeof( List<OktaSessionCache> ) )]
[JsonSerializable( typeof( UpdateCheckCache ) )]
[JsonSerializable( typeof( List<AwsCacheModel> ) )]
[JsonSerializable( typeof( OktaHomeResponse ) )]
internal partial class JsonCamelCaseContext : JsonSerializerContext {
}

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower )]
[JsonSerializable( typeof( GitHubRelease ) )]
internal partial class JsonSnakeCaseContext : JsonSerializerContext {
}
