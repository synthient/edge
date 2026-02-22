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
    MetricsReporter metrics
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
                    metrics.RecordUnmatched();
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