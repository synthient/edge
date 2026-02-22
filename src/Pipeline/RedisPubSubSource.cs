using System.Threading.Tasks.Dataflow;
using StackExchange.Redis;
using Synthient.Edge.Models;
using Synthient.Edge.Models.Config;
using Synthient.Edge.Serialization;

namespace Synthient.Edge.Pipeline;

public sealed class RedisPubSubSource(
    AppConfig appConfig,
    ITargetBlock<ProxyEvent> target,
    ILogger<RedisPubSubSource> logger
) : BackgroundService
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly RedisChannel _channel = RedisChannel.Literal(appConfig.Source.Channel);
    private readonly ConfigurationOptions _redisOptions = BuildRedisConfiguration(appConfig.Source);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                logger.LogCritical(ex, "Unexpected error in Redis source. The pipeline may be in a degraded state.");
                await Task.Delay(backoff, stoppingToken);
            }
        }

        target.Complete();
        logger.LogInformation("Redis source stopped.");
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken ct)
    {
        logger.LogInformation("Connecting to Redis source ({Endpoint})", appConfig.Source.Endpoint);
        await using var connection = await ConnectionMultiplexer.ConnectAsync(_redisOptions);

        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionFailed += (_, _) => disconnected.TrySetResult();

        var subscriber = connection.GetSubscriber();

        await subscriber.SubscribeAsync(_channel, (_, message) => OnMessage(message));
        logger.LogInformation("Subscribed to channel '{Channel}'.", _channel);

        await using var _ = ct.Register(() => disconnected.TrySetResult());
        await disconnected.Task;
    }

    private void OnMessage(RedisValue message)
    {
        if (message.IsNullOrEmpty) return;

        if (ProxyEventSerializer.TryDeserialize(message, out var evt))
            target.Post(evt);
        else
            logger.LogWarning("Failed to deserialize message: {Message}.", (string?)message);
    }

    private static ConfigurationOptions BuildRedisConfiguration(RedisSourceConfig config)
    {
        return new ConfigurationOptions
        {
            EndPoints = { config.Endpoint },
            Password = config.Password,
            Ssl = config.Ssl,

            // Ideally it internally reconnects.
            AbortOnConnectFail = false,
            ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds: 1000),
            ConnectRetry = 5
        };
    }
}