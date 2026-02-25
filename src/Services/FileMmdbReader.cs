using System.Net;
using MaxMind.Db;
using Synthient.Edge.Config;
using Synthient.Edge.Models;

namespace Synthient.Edge.Services;

public sealed class FileMmdbReader(AppConfig appConfig, ILogger<FileMmdbReader> logger) : IMmdbReader, IDisposable
{
    private readonly Reader _mmdbReader = new(appConfig.Mmdb.Path);

    public (MmdbNetwork Network, MmdbLocation Location) LookupNetworkAndLocation(IPAddress ipAddress)
    {
        var data = Lookup(ipAddress);

        var network = MmdbNetwork.From(data);
        var location = MmdbLocation.From(data);

        return (network, location);
    }

    public MmdbData Lookup(IPAddress ipAddress)
    {
        try
        {
            return _mmdbReader.Find<MmdbData>(ipAddress) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MMDB lookup failed for IP {Ip}.", ipAddress);
            return [];
        }
    }

    public void Dispose()
    {
        _mmdbReader.Dispose();
    }
}