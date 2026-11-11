using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Interfaces;

/// <remarks>
/// Every read takes the owner's id, so a query can never return another user's tasks by accident.
/// </remarks>
public interface ITaskRepository
{
    /// <summary>Returns one page of the user's tasks matching the filters, plus the total match count.</summary>
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        Guid userId, TaskQueryParameters query, CancellationToken ct = default);

    Task<IReadOnlyDictionary<TaskItemStatus, int>> CountByStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The user's tasks not done whose due date is before <paramref name="nowUtc"/>.</summary>
    Task<int> CountOverdueAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>Returns the task only if it belongs to <paramref name="userId"/>.</summary>
    Task<TaskItem?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task AddAsync(TaskItem task, CancellationToken ct = default);
    void Remove(TaskItem task);
    Task SaveChangesAsync(CancellationToken ct = default);
}
