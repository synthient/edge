namespace Synthient.Edge.Config.Definitions;

public interface IKeyedConfigDefinition<out T>
{
    T Build(string key);
}