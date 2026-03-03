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
    private readonly int _bucketsCount = appConfig.Buckets.Count;
    private readonly TimeSpan _maxBucketTtl = appConfig.MaxBucketTtl;
    private readonly FrozenDictionary<string, BucketConfig> _buckets = appConfig.Buckets;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var requiresMmdb = appConfig.FiltersRequireMmdb;

        try
        {
            await foreach (var evt in input.ReadAllAsync(stoppingToken))
            {
                var eventAge = DateTimeOffset.UtcNow - evt.Timestamp;
                if (eventAge >= _maxBucketTtl)
                {
                    metrics.RecordExpired();
                    continue;
                }

                var mmdbData = requiresMmdb
                    ? mmdbReader.Lookup(evt.IpAddress)
                    : null;

                if (!TryMatchBuckets(evt, mmdbData, eventAge, out var matches, out var matchCount))
                {
                    metrics.RecordUnmatched();
                    continue;
                }

                var bucketedEvent = new BucketedEvent(evt, matches, matchCount);
                await output.WriteAsync(bucketedEvent, stoppingToken);
            }
        }
        finally
        {
            output.Complete();
        }
    }

    // Intentionally not resizing matches array post-matching to avoid extra allocation.
    private bool TryMatchBuckets(
        ProxyEvent evt,
        MmdbData? mmdb,
        TimeSpan eventAge,
        [NotNullWhen(true)] out BucketMatch[]? matches,
        out int matchCount
    )
    {
        matches = null;
        matchCount = 0;

        foreach (var (name, bucket) in _buckets)
        {
            if (eventAge >= bucket.Ttl)
                continue;

            if (!bucket.Matches(evt, mmdb))
                continue;

            matches ??= new BucketMatch[_bucketsCount];
            matches[matchCount++] = new BucketMatch(name, bucket);
        }

        return matchCount > 0;
    }
}