using System.Net;
using System.Text.Json.Serialization;
using Synthient.Edge.Serialization;

namespace Synthient.Edge.Models;

// TODO: Deserialize timestamp directly to DateTimeOffset.
public sealed record ProxyEvent(
    [property: JsonPropertyName("ip"), JsonConverter(typeof(IpAddressJsonConverter))] IPAddress IpAddress,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("timestamp"), JsonConverter(typeof(LongFromStringJsonConverter))] long Timestamp
);