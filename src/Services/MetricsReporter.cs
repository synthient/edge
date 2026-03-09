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

    /// <summary>
    /// Records an event that was received from the source and accepted for processing.
    /// </summary>
    public void RecordIngested() => Interlocked.Increment(ref _ingested);

    /// <summary>
    /// Records bucketed events that were successfully processed and written to the sink.
    /// </summary>
    public void RecordProcessed() => Interlocked.Increment(ref _processed);

    /// <summary>
    /// Records an event that was discarded because it did not match any bucket's filter(s).
    /// </summary>
    public void RecordUnmatched() => Interlocked.Increment(ref _unmatched);

    /// <summary>
    /// Records an event that was lost at ingestion because the filter channel was full.
    /// </summary>
    public void RecordOverflow() => Interlocked.Increment(ref _overflow);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        using var timer = new PeriodicTimer(LogInterval);
        long lastIngested = 0, lastProcessed = 0, lastUnmatched = 0, lastOverflow = 0;

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var ingested = Volatile.Read(ref _ingested);
            var processed = Volatile.Read(ref _processed);
            var unmatched = Volatile.Read(ref _unmatched);
            var overflow = Volatile.Read(ref _overflow);
            var seconds = LogInterval.TotalSeconds;

            var ingestedDelta = ingested - lastIngested;
            var processedDelta = processed - lastProcessed;
            var unmatchedDelta = unmatched - lastUnmatched;
            var overflowDelta = overflow - lastOverflow;
            var unmatchedPct = ingestedDelta > 0 ? unmatchedDelta * 100.0 / ingestedDelta : 0;
            var lag = filterQueue.Count + sinkQueue.Count;

            LogThroughput(
                logger,
                ingestedRate: ingestedDelta / seconds,
                processedRate: processedDelta / seconds,
                unmatchedPct: unmatchedPct,
                overflow: overflowDelta,
                lag: lag
            );

            (lastIngested, lastProcessed, lastUnmatched, lastOverflow) = (ingested, processed, unmatched, overflow);
        }
    }

    [LoggerMessage(LogLevel.Information,
        "Ingested: {ingestedRate:N0}/s | " +
        "Processed: {processedRate:N0}/s | " +
        "Unmatched: {unmatchedPct:N1}% | " +
        "Overflow: {overflow:N0} | " +
        "Lag: {lag:N0}"
    )]
    private static partial void LogThroughput(
        ILogger logger,
        double ingestedRate,
        double processedRate,
        double unmatchedPct,
        long overflow,
        int lag
    );
}