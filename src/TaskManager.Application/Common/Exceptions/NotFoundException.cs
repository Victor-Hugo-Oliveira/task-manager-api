namespace TaskManager.Application.Common.Exceptions;

public class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} with id '{key}' was not found.");
