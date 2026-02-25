using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Synthient.Edge.Config;
using Synthient.Edge.Models;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed class BucketFilter(
    AppConfig appConfig,
    IMmdbReader mmdbReader,
    ChannelReader<ProxyEvent> input,
    ChannelWriter<BucketedEvent> output,
    MetricsReporter metrics
) : BackgroundService
{
    private readonly bool _requiresMmdb = appConfig.FiltersRequireMmdb;
    private readonly int _bucketsCount = appConfig.Buckets.Count;
    private readonly FrozenDictionary<string, BucketConfig> _buckets = appConfig.Buckets;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var evt in input.ReadAllAsync(stoppingToken))
            {
                var mmdbData = _requiresMmdb
                    ? mmdbReader.Lookup(evt.IpAddress)
                    : null;

                if (!TryMatchBuckets(evt, mmdbData, out var matched, out var matches))
                {
                    metrics.RecordUnmatched();
                    continue;
                }

                var bucketedEvent = new BucketedEvent(evt, matched, matches);
                await output.WriteAsync(bucketedEvent, stoppingToken);
            }
        }
        finally
        {
            output.Complete();
        }
    }

    private bool TryMatchBuckets(
        ProxyEvent evt,
        MmdbData? mmdb,
        [NotNullWhen(true)] out BucketMatch[]? matched,
        out int matches
    )
    {
        var buffer = BucketedEvent.RentFromPool(_bucketsCount);
        matches = 0;

        foreach (var (name, bucket) in _buckets)
            if (bucket.Matches(evt, mmdb))
                buffer[matches++] = new BucketMatch(name, bucket);

        if (matches == 0)
        {
            ArrayPool<BucketMatch>.Shared.Return(buffer);
            matched = null;
            return false;
        }

        matched = buffer;
        return true;
    }
}