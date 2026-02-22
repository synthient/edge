using System.Threading.Channels;
using Synthient.Edge.Models;
using Synthient.Edge.Models.Config;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed class BucketFilter(
    AppConfig appConfig,
    MmDbReader mmdbReader,
    ChannelReader<ProxyEvent> input,
    ChannelWriter<BucketedEvent> output,
    ILogger<BucketFilter> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var evt in input.ReadAllAsync(stoppingToken))
            {
                var mmdbData = appConfig.FiltersRequireMmdb
                    ? mmdbReader.Lookup(evt.IpAddress)
                    : null;

                if (!appConfig.TryMatchBuckets(evt, mmdbData, out var matched))
                {
                    logger.LogDebug("Event dropped: unmatched event with IP {Ip}.", evt.IpAddress);
                    continue;
                }

                await output.WriteAsync(new BucketedEvent(evt, matched), stoppingToken);
            }
        }
        finally
        {
            output.Complete();
        }
    }
}