using System.Net;
using MaxMind.Db;
using Synthient.Edge.Models;
using Synthient.Edge.Models.Config;

namespace Synthient.Edge.Services;

public class MmDbReader(AppConfig appConfig, ILogger<MmDbReader> logger) : IDisposable
{
    private readonly Reader _mmDbReader = new(appConfig.Mmdb.Path);

    public (MmdbNetwork Network, MmdbLocation Location) LookupNetworkAndLocation(IPAddress ipAddress)
    {
        try
        {
            var data = Lookup(ipAddress);

            var network = MmdbNetwork.From(data);
            var location = MmdbLocation.From(data);

            return (network, location);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MMDB lookup failed for IP {Ip}.", ipAddress);
            return (MmdbNetwork.Empty, MmdbLocation.Empty);
        }
    }

    public MmdbData Lookup(IPAddress ipAddress)
    {
        try
        {
            var mmDbData = _mmDbReader.Find<MmdbData>(ipAddress);
            return mmDbData ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MMDB lookup failed for IP {Ip}.", ipAddress);
            return [];
        }
    }

    public void Dispose()
    {
        _mmDbReader.Dispose();
        GC.SuppressFinalize(this);
    }
}