namespace Synthient.Edge.Config.Definitions;

public interface IConfigDefinition<out T>
{
    T Build();
}