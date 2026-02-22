namespace Synthient.Edge.Models.Config.Definitions;

public interface IKeyedConfigDefinition<out T>
{
    T Build(string key);
}