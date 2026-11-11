using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

/// <summary>
/// A task owned by the application. Named TaskItem to avoid clashing with System.Threading.Tasks.Task.
/// State changes go through methods so business rules live in one place.
/// </summary>
public class TaskItem
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Required by EF Core.
    private TaskItem() { }

    public TaskItem(string title, string? description, TaskPriority priority, DateTime? dueDate)
    {
        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = NormalizeDescription(description);
        Priority = priority;
        DueDate = dueDate;
        Status = TaskItemStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string? description, TaskPriority priority, DateTime? dueDate)
    {
        Title = title.Trim();
        Description = NormalizeDescription(description);
        Priority = priority;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TaskItemStatus status)
    {
        if (Status == status) return;

        Status = status;
        CompletedAt = status == TaskItemStatus.Done ? DateTime.UtcNow : null;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
