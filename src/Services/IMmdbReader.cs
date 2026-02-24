using System.Net;
using Synthient.Edge.Models;

namespace Synthient.Edge.Services;

public interface IMmdbReader
{
    (MmdbNetwork Network, MmdbLocation Location) LookupNetworkAndLocation(IPAddress ipAddress);
    MmdbData Lookup(IPAddress ipAddress);
}