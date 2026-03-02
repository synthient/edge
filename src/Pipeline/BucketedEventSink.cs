using System.Threading.Channels;
using Synthient.Edge.Models;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed partial class BucketedEventSink(
    ChannelReader<BucketedEvent> input,
    IEventRepository repo,
    MetricsReporter metrics,
    ILogger<BucketedEventSink> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var bucketedEvent in input.ReadAllAsync(stoppingToken))
        {
            try
            {
                await repo.InsertAsync(bucketedEvent, stoppingToken);
                metrics.RecordProcessed();
            }
            catch (Exception ex)
            {
                LogInsertFailed(logger, ex);
            }
        }

        LogStopped(logger);
    }

    [LoggerMessage(LogLevel.Error, "Failed to insert event into store.")]
    private static partial void LogInsertFailed(ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Event sink stopped.")]
    private static partial void LogStopped(ILogger logger);
}