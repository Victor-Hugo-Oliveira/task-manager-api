using FluentValidation;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default);
    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Validates input and throws <see cref="ValidationException"/> or <see cref="NotFoundException"/>;
/// the API's global exception handler turns those into 400/404 ProblemDetails responses.
/// </summary>
public class TaskService(
    ITaskRepository repository,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<UpdateTaskStatusRequest> statusValidator) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await repository.GetAllAsync(ct);
        return tasks.Select(TaskResponse.FromEntity).ToList();
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(id, ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var task = new TaskItem(request.Title, request.Description, request.Priority, request.DueDate);
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var task = await GetTaskOrThrowAsync(id, ct);
        task.Update(request.Title, request.Description, request.Priority, request.DueDate);
        await repository.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        await statusValidator.ValidateAndThrowAsync(request, ct);

        var task = await GetTaskOrThrowAsync(id, ct);
        task.ChangeStatus(request.Status);
        await repository.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetTaskOrThrowAsync(id, ct);
        repository.Remove(task);
        await repository.SaveChangesAsync(ct);
    }

    private async Task<TaskItem> GetTaskOrThrowAsync(Guid id, CancellationToken ct) =>
        await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Task", id);
}
