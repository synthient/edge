using System.Threading.Channels;
using Synthient.Edge.Models;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed class RedisSink(
    ChannelReader<BucketedEvent> input,
    MetricsReporter metrics,
    ILogger<RedisSink> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var bucketedEvt in input.ReadAllAsync(stoppingToken))
        {
            metrics.RecordProcessed();
        }

        logger.LogInformation("Redis sink stopped.");
    }
}