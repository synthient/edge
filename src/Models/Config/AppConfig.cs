namespace Synthient.Edge.Models.Config;

public class AppConfig
{
    public ServerConfig Server { get; set; } = new();
    public List<string> ApiKeys { get; set; } = [];
}