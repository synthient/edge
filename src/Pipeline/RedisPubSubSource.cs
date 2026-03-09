using System.Threading.Channels;
using StackExchange.Redis;
using Synthient.Edge.Config;
using Synthient.Edge.Models;
using Synthient.Edge.Serialization;
using Synthient.Edge.Services;

namespace Synthient.Edge.Pipeline;

public sealed partial class RedisPubSubSource(
    AppConfig appConfig,
    ChannelWriter<ProxyEvent> output,
    MetricsReporter metrics,
    IHostEnvironment env,
    ILogger<RedisPubSubSource> logger
) : BackgroundService
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly RedisChannel _channel = RedisChannel.Literal(appConfig.Source.Channel);
    private readonly ConfigurationOptions _redisOptions = BuildRedisConfiguration(appConfig.Source, env.IsDevelopment());

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
                    LogConnectionFailed(logger, ex, backoff);
                    await Task.Delay(backoff, stoppingToken);
                    backoff = backoff * 2 < MaxBackoff ? backoff * 2 : MaxBackoff;
                }
                catch (Exception ex)
                {
                    LogUnexpectedError(logger, ex);
                    await Task.Delay(backoff, stoppingToken);
                }
            }
        }
        finally
        {
            output.Complete();
            LogSourceStopped(logger);
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
    {
        LogConnecting(logger, appConfig.Source.Endpoint);
        await using var connection = await ConnectionMultiplexer.ConnectAsync(_redisOptions);

        var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionFailed += (_, _) => disconnected.TrySetResult(); // TODO: Log on connection failure?

        var subscriber = connection.GetSubscriber();

        await subscriber.SubscribeAsync(_channel, (_, message) => OnMessage(message));
        LogSubscribed(logger, _channel);

        await using var _ = cancellationToken.Register(() => disconnected.TrySetResult());
        await disconnected.Task;
    }

    private void OnMessage(RedisValue message)
    {
        if (message.IsNullOrEmpty) return;

        if (!ProxyEventSerializer.TryDeserialize(message, out var evt))
        {
            LogFailedDeserialization(logger, message);
            return;
        }

        metrics.RecordIngested();

        if (!output.TryWrite(evt))
            metrics.RecordOverflow();
    }

    private static ConfigurationOptions BuildRedisConfiguration(RedisSourceConfig config, bool isDevelopment) => new()
    {
        EndPoints = { config.Endpoint },
        Password = config.Password,
        Ssl = config.Ssl,
        AbortOnConnectFail = false,
        ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds: 1000),
        ConnectRetry = 5,
        IncludeDetailInExceptions = isDevelopment
    };

    [LoggerMessage(LogLevel.Error, "Redis connection failed. Reconnecting in {backoff:g}.")]
    private static partial void LogConnectionFailed(ILogger<RedisPubSubSource> logger, Exception ex, TimeSpan backoff);

    [LoggerMessage(LogLevel.Critical, "Unexpected error in Redis source. The pipeline may be degraded.")]
    private static partial void LogUnexpectedError(ILogger<RedisPubSubSource> logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Redis source stopped.")]
    private static partial void LogSourceStopped(ILogger<RedisPubSubSource> logger);

    [LoggerMessage(LogLevel.Information, "Connecting to Redis source ({endpoint})")]
    private static partial void LogConnecting(ILogger<RedisPubSubSource> logger, string endpoint);

    [LoggerMessage(LogLevel.Information, "Subscribed to Redis channel '{channel}'.")]
    private static partial void LogSubscribed(ILogger<RedisPubSubSource> logger, RedisChannel channel);

    [LoggerMessage(LogLevel.Warning, "Failed to deserialize message: {message}.")]
    private static partial void LogFailedDeserialization(ILogger<RedisPubSubSource> logger, string? message);
}