using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    // Enums are stored as text, so ordering by the column would be alphabetical
    // (High < Low < Medium). These rank expressions sort by meaning instead; EF translates them to CASE.
    private static readonly Expression<Func<TaskItem, int>> PriorityRank = t =>
        t.Priority == TaskPriority.High ? 2 : t.Priority == TaskPriority.Medium ? 1 : 0;

    private static readonly Expression<Func<TaskItem, int>> StatusRank = t =>
        t.Status == TaskItemStatus.Done ? 2 : t.Status == TaskItemStatus.InProgress ? 1 : 0;

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        Guid userId, TaskQueryParameters query, CancellationToken ct = default)
    {
        var tasks = db.Tasks.AsNoTracking().Where(t => t.UserId == userId);

        if (query.Status is { } status)
            tasks = tasks.Where(t => t.Status == status);

        if (query.Priority is { } priority)
            tasks = tasks.Where(t => t.Priority == priority);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            tasks = tasks.Where(t =>
                t.Title.ToLower().Contains(term) ||
                (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        var totalCount = await tasks.CountAsync(ct);

        var items = await ApplySorting(tasks, query.SortBy, query.SortDirection)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<TaskItemStatus, int>> CountByStatusAsync(Guid userId, CancellationToken ct = default) =>
        await db.Tasks
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

    public Task<int> CountOverdueAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default) =>
        db.Tasks.CountAsync(t =>
            t.UserId == userId &&
            t.Status != TaskItemStatus.Done &&
            t.DueDate != null &&
            t.DueDate < nowUtc, ct);

    public Task<TaskItem?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

    public async Task AddAsync(TaskItem task, CancellationToken ct = default) =>
        await db.Tasks.AddAsync(task, ct);

    public void Remove(TaskItem task) => db.Tasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static IQueryable<TaskItem> ApplySorting(
        IQueryable<TaskItem> tasks, TaskSortField sortBy, SortDirection direction)
    {
        var desc = direction == SortDirection.Desc;

        var ordered = sortBy switch
        {
            TaskSortField.Title => Order(tasks, t => t.Title.ToLower(), desc),
            TaskSortField.Priority => Order(tasks, PriorityRank, desc),
            TaskSortField.Status => Order(tasks, StatusRank, desc),
            // Tasks without a due date always go last, whatever the direction.
            TaskSortField.DueDate => desc
                ? tasks.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate)
                : tasks.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
            _ => Order(tasks, t => t.CreatedAt, desc)
        };

        // Tie-breakers keep pagination stable when the sort key has duplicates.
        return ordered.ThenByDescending(t => t.CreatedAt).ThenBy(t => t.Id);
    }

    private static IOrderedQueryable<TaskItem> Order<TKey>(
        IQueryable<TaskItem> tasks, Expression<Func<TaskItem, TKey>> key, bool desc) =>
        desc ? tasks.OrderByDescending(key) : tasks.OrderBy(key);
}
