using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.UnitTests.Services;

public class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(_userId);
        var time = new FakeTimeProvider(Now);

        // Real validators: the service's contract includes "invalid input throws".
        _service = new TaskService(
            _repository,
            new CreateTaskRequestValidator(time),
            new UpdateTaskRequestValidator(),
            new UpdateTaskStatusRequestValidator(),
            new TaskQueryParametersValidator(),
            time,
            currentUser);
    }

    private TaskItem ExistingTask()
    {
        var task = new TaskItem(_userId, "Existing", null, TaskPriority.Low, null);
        _repository.GetByIdAsync(_userId, task.Id, Arg.Any<CancellationToken>()).Returns(task);
        return task;
    }

    [Fact]
    public async Task Create_saves_a_task_owned_by_the_current_user()
    {
        var result = await _service.CreateAsync(new CreateTaskRequest("New", null, TaskPriority.High));

        await _repository.Received(1).AddAsync(
            Arg.Is<TaskItem>(t => t.UserId == _userId && t.Title == "New"), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("New", result.Title);
        Assert.Equal(TaskItemStatus.Pending, result.Status);
    }

    [Fact]
    public async Task Create_with_invalid_input_throws_and_saves_nothing()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(new CreateTaskRequest("", null)));

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_unknown_task_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Lookups_always_use_the_current_users_id()
    {
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));

        await _repository.Received(1).GetByIdAsync(_userId, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_status_to_done_completes_the_task()
    {
        var task = ExistingTask();

        var result = await _service.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest(TaskItemStatus.Done));

        Assert.Equal(TaskItemStatus.Done, result.Status);
        Assert.NotNull(result.CompletedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_removes_existing_task()
    {
        var task = ExistingTask();

        await _service.DeleteAsync(task.Id);

        _repository.Received(1).Remove(task);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_unknown_task_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
        _repository.DidNotReceive().Remove(Arg.Any<TaskItem>());
    }

    [Fact]
    public async Task Paged_response_carries_paging_metadata()
    {
        var query = new TaskQueryParameters { Page = 2, PageSize = 5 };
        var items = new[] { new TaskItem(_userId, "A", null, TaskPriority.Low, null) };
        _repository.GetPagedAsync(_userId, query, Arg.Any<CancellationToken>()).Returns((items, 11));

        var page = await _service.GetPagedAsync(query);

        Assert.Single(page.Items);
        Assert.Equal(11, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task Summary_computes_totals_and_completion_rate()
    {
        _repository.CountByStatusAsync(_userId, Arg.Any<CancellationToken>()).Returns(
            new Dictionary<TaskItemStatus, int>
            {
                [TaskItemStatus.Pending] = 2,
                [TaskItemStatus.InProgress] = 1,
                [TaskItemStatus.Done] = 1
            });
        _repository.CountOverdueAsync(_userId, Now.UtcDateTime, Arg.Any<CancellationToken>()).Returns(1);

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(new TaskSummaryResponse(4, 2, 1, 1, 1, 25.0), summary);
    }

    [Fact]
    public async Task Summary_with_no_tasks_has_zero_completion_rate()
    {
        _repository.CountByStatusAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TaskItemStatus, int>());

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.Total);
        Assert.Equal(0, summary.CompletionRate);
    }
}
