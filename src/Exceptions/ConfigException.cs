namespace Synthient.Edge.Exceptions;

public class ConfigException(string message, Exception? innerException = null) : Exception(message, innerException);