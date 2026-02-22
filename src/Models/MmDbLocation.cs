using Synthient.Edge.Extensions;

namespace Synthient.Edge.Models;

public sealed record MmdbLocation(string? CountryCode, string? City, string? Timezone)
{
    public static MmdbLocation Empty => new(null, null, null);
    
    public static MmdbLocation From(MmdbData data) => new(
        CountryCode: data.TryFind<string>("country", "iso_code"),
        City:        data.TryFind<string>("city", "names", "en"),
        Timezone:    data.TryFind<string>("location", "time_zone")
    );
}