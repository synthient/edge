using System.Text.Json;
using System.Threading.Channels;
using StackExchange.Redis;
using Synthient.Edge.Endpoints;
using Synthient.Edge.Models;
using Synthient.Edge.Pipeline;
using Synthient.Edge.Services;
using Synthient.Edge.Utilities;

var appConfig = AppConfigLoader.Load(args);

#region Builder

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseUrls($"http://{appConfig.Server.Host}:{appConfig.Server.Port}");

// Config.
builder.Services.AddSingleton(appConfig);
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
);

// Infra.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(new ConfigurationOptions
{
    EndPoints = { appConfig.Sink.Endpoint },
    Password = appConfig.Sink.Password,
    Ssl = appConfig.Sink.Ssl,
    DefaultDatabase = appConfig.Sink.Database,
    AbortOnConnectFail = false,
    ConnectRetry = 5,
    IncludeDetailInExceptions = builder.Environment.IsDevelopment(),
    ConnectTimeout = 5_000,
    SyncTimeout = 5_000,
}));

builder.Services.AddSingleton<IMmdbReader, FileMmdbReader>();
builder.Services.AddSingleton<IEventRepository, RedisEventRepository>();
builder.Services.AddSingleton<IStringRegistry, RedisStringRegistry>();
builder.Services.AddHostedService<StringRegistryInitializer>();

// Pipeline.
var eventChannel = Channel.CreateBounded<ProxyEvent>(new BoundedChannelOptions(250_000)
{
    FullMode = BoundedChannelFullMode.DropWrite,
    SingleWriter = true,
    SingleReader = true
});

var bucketedEventChannel = Channel.CreateBounded<BucketedEvent>(new BoundedChannelOptions(250_000)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
    SingleReader = true
});

builder.Services.AddSingleton(eventChannel.Writer);
builder.Services.AddSingleton(eventChannel.Reader);
builder.Services.AddSingleton(bucketedEventChannel.Writer);
builder.Services.AddSingleton(bucketedEventChannel.Reader);

builder.Services.AddHostedService<RedisPubSubSource>();
builder.Services.AddHostedService<BucketFilter>();
builder.Services.AddHostedService<BucketedEventSink>();

// Metrics
builder.Services.AddSingleton<MetricsReporter>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MetricsReporter>());

#endregion

#region App

var app = builder.Build();
app.MapContextEndpoints(appConfig);
app.MapHealthEndpoint(appConfig);

#endregion

app.Run();