using System.Threading.Channels;
using Synthient.Edge.Models;

namespace Synthient.Edge.Services;

/// <summary>
/// A background service that collects and periodically logs metrics pertaining to the whole event processing pipeline.
/// </summary>
public sealed partial class MetricsReporter(
    ILogger<MetricsReporter> logger,
    ChannelReader<ProxyEvent> filterQueue,
    ChannelReader<BucketedEvent> sinkQueue
) : BackgroundService
{
    private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(5);

    private long _ingested;
    private long _processed;
    private long _unmatched;
    private long _overflow;
    private long _expired;

    /// <summary>
    /// Records an event that was received from the source and accepted for processing.
    /// </summary>
    public void RecordIngested() => Interlocked.Increment(ref _ingested);

    /// <summary>
    /// Records bucketed events that were successfully processed and written to the sink.
    /// </summary>
    public void RecordProcessed() => Interlocked.Increment(ref _processed);

    /// <summary>
    /// Records an event that was discarded by the filter because it did not match any bucket's filter criteria(s).
    /// </summary>
    public void RecordUnmatched() => Interlocked.Increment(ref _unmatched);

    /// <summary>
    /// Records an event that was lost at ingestion because the filter channel was full, indicating that the system is under backpressure and cannot keep up with the incoming event rate.
    /// </summary>
    public void RecordOverflow() => Interlocked.Increment(ref _overflow);

    /// <summary>
    /// Records an event that was discarded by the filter because it was older than any bucket TTL.
    /// </summary>
    public void RecordExpired() => Interlocked.Increment(ref _expired);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;

        using var timer = new PeriodicTimer(LogInterval);
        long lastIngested = 0, lastProcessed = 0, lastUnmatched = 0, lastOverflow = 0, lastExpired = 0;

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var ingested = Volatile.Read(ref _ingested);
            var processed = Volatile.Read(ref _processed);
            var unmatched = Volatile.Read(ref _unmatched);
            var overflow = Volatile.Read(ref _overflow);
            var expired = Volatile.Read(ref _expired);
            var seconds = LogInterval.TotalSeconds;

            var ingestedDelta = ingested - lastIngested;
            var processedDelta = processed - lastProcessed;
            var unmatchedDelta = unmatched - lastUnmatched;
            var overflowDelta = overflow - lastOverflow;
            var expiredDelta = expired - lastExpired;
            var unmatchedPerc = ingestedDelta > 0 ? (double)unmatchedDelta / ingestedDelta * 100 : 0;

            LogThroughput(
                logger,
                ingestedRate: ingestedDelta / seconds,
                processedRate: processedDelta / seconds,
                ingestedDelta,
                processedDelta,
                unmatched: unmatchedDelta,
                unmatchedPerc,
                overflow: overflowDelta,
                expired: expiredDelta,
                filterQueueCount: filterQueue.Count,
                sinkQueueCount: sinkQueue.Count
            );

            (lastIngested, lastProcessed, lastUnmatched, lastOverflow, lastExpired) =
                (ingested, processed, unmatched, overflow, expired);
        }
    }

    [LoggerMessage(LogLevel.Information,
        "Ingested: {ingestedRate:N0}/s (+{ingestedDelta:N0}) | " +
        "Processed: {processedRate:N0}/s (+{processedDelta:N0}) | " +
        "Unmatched: {unmatched:N0} ({unmatchedPct:N1}%) | " +
        "Overflow: {overflow:N0} | " +
        "Expired: {expired:N0} | " +
        "ToFilter: {filterQueueCount:N0} | ToSink: {sinkQueueCount:N0}"
    )]
    private static partial void LogThroughput(
        ILogger logger,
        double ingestedRate,
        double processedRate,
        long ingestedDelta,
        long processedDelta,
        long unmatched,
        double unmatchedPct,
        long overflow,
        long expired,
        int filterQueueCount,
        int sinkQueueCount
    );
}