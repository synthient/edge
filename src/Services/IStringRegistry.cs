namespace Synthient.Edge.Services;

public interface IStringRegistry
{
    ValueTask<int> GetOrCreateIdAsync(string value, CancellationToken cancellationToken);
    ValueTask<string> GetStringAsync(int id, CancellationToken cancellationToken);
    Task WarmAsync();
}