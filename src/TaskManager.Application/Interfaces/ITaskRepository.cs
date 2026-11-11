using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Interfaces;

public interface ITaskRepository
{
    /// <summary>Returns one page of tasks matching the filters, plus the total match count.</summary>
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(TaskQueryParameters query, CancellationToken ct = default);

    Task<IReadOnlyDictionary<TaskItemStatus, int>> CountByStatusAsync(CancellationToken ct = default);

    /// <summary>Tasks not done whose due date is before <paramref name="nowUtc"/>.</summary>
    Task<int> CountOverdueAsync(DateTime nowUtc, CancellationToken ct = default);

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    void Remove(TaskItem task);
    Task SaveChangesAsync(CancellationToken ct = default);
}
