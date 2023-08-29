using System.Text.Json.Serialization;
using D2L.Bmx.Okta.Models;
using D2L.Bmx.Aws;
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
[JsonSerializable( typeof( List<AwsCacheModel> ) )]
internal partial class SourceGenerationContext : JsonSerializerContext {
}
