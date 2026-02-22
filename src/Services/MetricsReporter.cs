namespace Synthient.Edge.Services;

public sealed partial class MetricsReporter(ILogger<MetricsReporter> logger) : BackgroundService
{
    private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(5);

    private long _ingested;
    private long _processed;
    private long _unmatched;
    private long _dropped;

    public void RecordIngested() => Interlocked.Increment(ref _ingested);
    public void RecordProcessed() => Interlocked.Increment(ref _processed);
    public void RecordUnmatched() => Interlocked.Increment(ref _unmatched);
    public void RecordDropped() => Interlocked.Increment(ref _dropped);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;

        using var timer = new PeriodicTimer(LogInterval);
        long lastIngested = 0, lastProcessed = 0, lastUnmatched = 0;

        while (await timer.WaitForNextTickAsync(ct))
        {
            var ingested = Volatile.Read(ref _ingested);
            var processed = Volatile.Read(ref _processed);
            var unmatched = Volatile.Read(ref _unmatched);
            var dropped = Volatile.Read(ref _dropped);
            var seconds = LogInterval.TotalSeconds;

            var ingestedDelta = ingested - lastIngested;
            var processedDelta = processed - lastProcessed;
            var unmatchedDelta = unmatched - lastUnmatched;
            var unmatchedPerc = ingestedDelta > 0 ? (double)unmatchedDelta / ingestedDelta * 100 : 0;

            LogThroughput(
                logger,
                ingestedRate: ingestedDelta / seconds,
                processedRate: processedDelta / seconds,
                ingestedDelta,
                processedDelta,
                unmatched: unmatchedDelta,
                unmatchedPerc,
                dropped,
                lag: ingested - processed - dropped - unmatched
            );

            (lastIngested, lastProcessed, lastUnmatched) = (ingested, processed, unmatched);
        }
    }

    [LoggerMessage(LogLevel.Information,
        "Ingested: {ingestedRate:N0}/s (+{ingestedDelta:N0}) | " +
        "Processed: {processedRate:N0}/s (+{processedDelta:N0}) | " +
        "Unmatched: {unmatched:N0} ({unmatchedPct:N1}%) | " +
        "Dropped: {dropped:N0} | " +
        "Lag: {lag:N0}")
    ]
    private static partial void LogThroughput(
        ILogger logger,
        double ingestedRate,
        double processedRate,
        long ingestedDelta,
        long processedDelta,
        long unmatched,
        double unmatchedPct,
        long dropped,
        long lag
    );
}