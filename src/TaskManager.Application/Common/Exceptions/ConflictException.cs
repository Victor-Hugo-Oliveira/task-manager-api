namespace TaskManager.Application.Common.Exceptions;

/// <summary>The request conflicts with existing state (e.g. e-mail already registered). Mapped to 409.</summary>
public class ConflictException(string message) : Exception(message);
