using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default);
    Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse?> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse?> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class TaskService(ITaskRepository repository) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await repository.GetAllAsync(ct);
        return tasks.Select(TaskResponse.FromEntity).ToList();
    }

    public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);
        return task is null ? null : TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem(request.Title, request.Description, request.Priority, request.DueDate);
        await repository.AddAsync(task, ct);
        await repository.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse?> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);
        if (task is null) return null;

        task.Update(request.Title, request.Description, request.Priority, request.DueDate);
        await repository.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse?> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);
        if (task is null) return null;

        task.ChangeStatus(request.Status);
        await repository.SaveChangesAsync(ct);
        return TaskResponse.FromEntity(task);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(id, ct);
        if (task is null) return false;

        repository.Remove(task);
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
