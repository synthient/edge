using System.Text.RegularExpressions;
using Synthient.Edge.Exceptions;
using Synthient.Edge.Models.Config;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Synthient.Edge.Utilities;

public sealed partial class AppConfigLoader
{
    [GeneratedRegex("Property '(.+?)' not found on type '.+?'", RegexOptions.Compiled)]
    private static partial Regex UnknownPropertyPattern();

    public static AppConfig Load(string[] args)
    {
        try
        {
            var path = GetConfigPath(args);
            var text = ReadConfigFile(path);
            return Deserialize(text);
        }
        catch (ConfigException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
            return null!;
        }
    }

    private static AppConfig Deserialize(string text)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithDuplicateKeyChecking()
            .Build();

        try
        {
            return deserializer.Deserialize<AppConfig>(text);
        }
        catch (YamlException ex)
        {
            var message = UnknownPropertyPattern().Match(ex.Message) is { Success: true } match
                ? $"Unknown config property '{match.Groups[1].Value}'."
                : ex.Message;

            throw new ConfigException(
                $"Invalid YAML at line {ex.Start.Line}, column {ex.Start.Column}: {message}", ex
            );
        }
        catch (Exception ex)
        {
            throw new ConfigException($"Unexpected error while reading config: {ex.Message}");
        }
    }

    private static string GetConfigPath(string[] args)
    {
        var path = args.ElementAtOrDefault(0);

        if (string.IsNullOrWhiteSpace(path))
            throw new ConfigException("First argument must be a path to a YAML config file.");

        if (!File.Exists(path))
            throw new ConfigException($"Config file not found: '{path}'");

        return path;
    }


    private static string ReadConfigFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new ConfigException($"Failed to read config file at '{path}': {ex.Message}", ex);
        }
    }
}