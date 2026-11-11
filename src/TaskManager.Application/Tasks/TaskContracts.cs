using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks;

// Request validation lives in TaskValidators.cs (FluentValidation).

public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority = TaskPriority.Medium,
    DateTime? DueDate = null);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueDate);

public record UpdateTaskStatusRequest(TaskItemStatus Status);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? CompletedAt)
{
    public static TaskResponse FromEntity(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.DueDate, t.CreatedAt, t.UpdatedAt, t.CompletedAt);
}
