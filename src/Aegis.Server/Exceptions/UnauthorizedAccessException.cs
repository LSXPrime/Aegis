namespace Aegis.Server.Exceptions;

public class UnauthorizedAccessException(string message) : ApiException(message, 401);