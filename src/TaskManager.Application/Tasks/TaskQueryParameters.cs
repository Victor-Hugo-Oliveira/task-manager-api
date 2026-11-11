using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks;

public enum TaskSortField
{
    CreatedAt,
    DueDate,
    Priority,
    Title,
    Status
}

public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>Query-string options for GET /api/tasks.</summary>
public record TaskQueryParameters
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;

    /// <summary>Only tasks with this status.</summary>
    public TaskItemStatus? Status { get; init; }

    /// <summary>Only tasks with this priority.</summary>
    public TaskPriority? Priority { get; init; }

    /// <summary>Case-insensitive text matched against title and description.</summary>
    public string? Search { get; init; }

    public TaskSortField SortBy { get; init; } = TaskSortField.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    /// <summary>1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Items per page (1–100).</summary>
    public int PageSize { get; init; } = 10;
}

public record TaskSummaryResponse(
    int Total,
    int Pending,
    int InProgress,
    int Done,
    int Overdue,
    double CompletionRate);
