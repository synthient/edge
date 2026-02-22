namespace Synthient.Edge.Services;

public class StringRegistryInitializer(IStringRegistry registry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => registry.WarmAsync();

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}