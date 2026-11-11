namespace TaskManager.Application.Common.Exceptions;

/// <summary>Authentication failed (e.g. wrong credentials). Mapped to 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
