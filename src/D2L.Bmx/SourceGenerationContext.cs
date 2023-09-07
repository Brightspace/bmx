using System.Text.Json.Serialization;
using D2L.Bmx.Okta.Models;

namespace D2L.Bmx;

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof( AuthenticateRequest ) )]
[JsonSerializable( typeof( IssueMfaChallengeRequest ) )]
[JsonSerializable( typeof( VerifyMfaChallengeResponseRequest ) )]
[JsonSerializable( typeof( CreateSessionRequest ) )]
[JsonSerializable( typeof( AuthenticateResponseRaw ) )]
[JsonSerializable( typeof( OktaSession ) )]
[JsonSerializable( typeof( OktaApp[] ) )]
[JsonSerializable( typeof( OktaMeResponse ) )]
[JsonSerializable( typeof( List<OktaSessionCache> ) )]
[JsonSerializable( typeof( GithubRelease ) )]
[JsonSerializable( typeof( UpdateCheckCache ) )]
internal partial class SourceGenerationContext : JsonSerializerContext {
}
