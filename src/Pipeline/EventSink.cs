using System.Threading.Channels;
using Synthient.Edge.Models;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed partial class EventSink(
    ChannelReader<BucketedEvent> input,
    IEventRepository repo,
    MetricsReporter metrics,
    ILogger<EventSink> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var bucketed in input.ReadAllAsync(stoppingToken))
        {
            try
            {
                await repo.InsertAsync(bucketed, stoppingToken);
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