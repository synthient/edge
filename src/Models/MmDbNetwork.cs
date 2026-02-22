using Synthient.Edge.Extensions;

namespace Synthient.Edge.Models;

public sealed record MmdbNetwork(long? Asn, string? Isp, string? Organization, string? UserType)
{
    public static MmdbNetwork Empty => new(null, null, null, null);
    
    public static MmdbNetwork From(MmdbData data) => new(
        Asn: data.TryFind<long?>("traits", "autonomous_system_number"),
        Isp: data.TryFind<string>("traits", "isp"),
        Organization: data.TryFind<string>("traits", "organization"),
        UserType: data.TryFind<string>("traits", "user_type")?.ToUpperInvariant()
    );
}