namespace Aegis.Server.Exceptions;

public class NotFoundException(string message) : ApiException(message, 404);