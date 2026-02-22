using System.Net;
using System.Text.Json.Serialization;
using Synthient.Edge.Serialization;

namespace Synthient.Edge.Models;

public sealed record ProxyEvent(
    [property: JsonPropertyName("ip"), JsonConverter(typeof(IpAddressJsonConverter))] IPAddress IpAddress,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("timestamp"), JsonConverter(typeof(LongFromStringConverter))] long Timestamp
);