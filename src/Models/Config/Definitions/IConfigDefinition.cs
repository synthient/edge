namespace Synthient.Edge.Models.Config.Definitions;

public interface IConfigDefinition<out T>
{
    T Build();
}