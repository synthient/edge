using Synthient.Edge.Models.Config.Definitions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Synthient.Edge.Serialization;

public sealed class FilterDefinitionDeserializer(INodeDeserializer inner) : INodeDeserializer
{
    public bool Deserialize(
        IParser parser,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value,
        ObjectDeserializer rootDeserializer
    )
    {
        if (expectedType != typeof(FilterDefinition))
            return inner.Deserialize(parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer);

        var definition = new FilterDefinition();

        parser.Consume<MappingStart>();

        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var key = parser.Consume<Scalar>().Value;

            if (key.Equals("provider", StringComparison.OrdinalIgnoreCase))
                definition.Provider = ReadScalarOrSequence(parser);
            else
                definition.MmdbFilters[key] = ReadScalarOrSequence(parser);
        }

        value = definition;
        return true;
    }

    private static List<string> ReadScalarOrSequence(IParser parser)
    {
        if (parser.Current is not SequenceStart)
            return [parser.Consume<Scalar>().Value];

        parser.Consume<SequenceStart>();
        var list = new List<string>();

        while (!parser.TryConsume<SequenceEnd>(out _))
            list.Add(parser.Consume<Scalar>().Value);

        return list;
    }
}