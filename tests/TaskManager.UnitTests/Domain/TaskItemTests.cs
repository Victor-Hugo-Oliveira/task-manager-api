using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.UnitTests.Domain;

public class TaskItemTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static TaskItem NewTask(string title = "Title", string? description = null) =>
        new(OwnerId, title, description, TaskPriority.Medium, dueDate: null);

    [Fact]
    public void New_task_starts_pending_and_belongs_to_owner()
    {
        var task = NewTask();

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(OwnerId, task.UserId);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Null(task.CompletedAt);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void Title_and_description_are_trimmed()
    {
        var task = NewTask("  Write tests  ", "  some details ");

        Assert.Equal("Write tests", task.Title);
        Assert.Equal("some details", task.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_description_becomes_null(string? description)
    {
        Assert.Null(NewTask(description: description).Description);
    }

    [Fact]
    public void Marking_done_sets_completed_at()
    {
        var task = NewTask();

        task.ChangeStatus(TaskItemStatus.Done);

        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.NotNull(task.CompletedAt);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Reopening_a_done_task_clears_completed_at()
    {
        var task = NewTask();
        task.ChangeStatus(TaskItemStatus.Done);

        task.ChangeStatus(TaskItemStatus.InProgress);

        Assert.Equal(TaskItemStatus.InProgress, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Setting_the_same_status_changes_nothing()
    {
        var task = NewTask();

        task.ChangeStatus(TaskItemStatus.Pending);

        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void Update_replaces_fields_and_sets_updated_at()
    {
        var task = NewTask();
        var due = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        task.Update("New title", "New description", TaskPriority.High, due);

        Assert.Equal("New title", task.Title);
        Assert.Equal("New description", task.Description);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(due, task.DueDate);
        Assert.NotNull(task.UpdatedAt);
    }
}
