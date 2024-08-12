namespace Aegis.Server.Exceptions;

public class BadRequestException(string message) : ApiException(message, 400);