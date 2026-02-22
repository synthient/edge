using System.Text.Json.Serialization;
using Synthient.Edge.Models;

namespace Synthient.Edge.Serialization;

[JsonSerializable(typeof(ProxyEvent))]
[JsonConverter(typeof(IpAddressJsonConverter))]
public sealed partial class ProxyEventJsonContext : JsonSerializerContext;