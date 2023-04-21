using System.Text.Json.Serialization;
using D2L.Bmx.Okta.Models;
namespace D2L.Bmx.Okta;

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof( AuthenticateOptions ) )]
[JsonSerializable( typeof( AuthenticateChallengeMfaOptions ) )]
[JsonSerializable( typeof( SessionOptions ) )]
[JsonSerializable( typeof( AuthenticateResponseInital ) )]
[JsonSerializable( typeof( AuthenticateResponseSuccess ) )]
[JsonSerializable( typeof( OktaSession ) )]
[JsonSerializable( typeof( OktaApp[] ) )]
[JsonSerializable( typeof( OktaMeResponse ) )]
[JsonSerializable( typeof( OktaSessionCache[] ) )]
internal partial class SourceGenerationContext : JsonSerializerContext {
}
