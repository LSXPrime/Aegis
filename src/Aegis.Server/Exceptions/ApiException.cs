namespace Aegis.Server.Exceptions;

public class ApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; private set; } = statusCode;
    public override string Message { get; } = message;
}