using System.Threading.Channels;
using StackExchange.Redis;
using Synthient.Edge.Models;
using Synthient.Edge.Models.Config;
using Synthient.Edge.Serialization;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed class RedisPubSubSource(
    AppConfig appConfig,
    ChannelWriter<ProxyEvent> output,
    MetricsReporter metrics,
    ILogger<RedisPubSubSource> logger
) : BackgroundService
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly RedisChannel _channel = RedisChannel.Literal(appConfig.Source.Channel);
    private readonly ConfigurationOptions _redisOptions = BuildRedisConfiguration(appConfig.Source);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var backoff = MinBackoff;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndSubscribeAsync(stoppingToken);
                    backoff = MinBackoff;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (RedisException ex)
                {
                    logger.LogError(ex, "Redis connection failed. Reconnecting in {Backoff:g}.", backoff);
                    await Task.Delay(backoff, stoppingToken);
                    backoff = backoff * 2 < MaxBackoff ? backoff * 2 : MaxBackoff;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Unexpected error in Redis source. The pipeline may be degraded.");
                    await Task.Delay(backoff, stoppingToken);
                }
            }
        }
        finally
        {
            output.Complete();
            logger.LogInformation("Redis source stopped.");
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting to Redis source ({Endpoint})", appConfig.Source.Endpoint);
        await using var connection = await ConnectionMultiplexer.ConnectAsync(_redisOptions);

        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionFailed += (_, _) => disconnected.TrySetResult();

        var subscriber = connection.GetSubscriber();

        await subscriber.SubscribeAsync(_channel, (_, message) => OnMessage(message));
        logger.LogInformation("Subscribed to channel '{Channel}'.", _channel);

        await using var _ = cancellationToken.Register(() => disconnected.TrySetResult());
        await disconnected.Task;
    }

    private void OnMessage(RedisValue message)
    {
        if (message.IsNullOrEmpty) return;

        if (!ProxyEventSerializer.TryDeserialize(message, out var evt))
        {
            logger.LogWarning("Failed to deserialize message: {Message}.", (string?)message);
            return;
        }

        metrics.RecordIngested();

        if (!output.TryWrite(evt))
            metrics.RecordDropped();
    }

    private static ConfigurationOptions BuildRedisConfiguration(RedisSourceConfig config) => new()
    {
        EndPoints = { config.Endpoint },
        Password = config.Password,
        Ssl = config.Ssl,
        AbortOnConnectFail = false,
        ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds: 1000),
        ConnectRetry = 5
    };
}