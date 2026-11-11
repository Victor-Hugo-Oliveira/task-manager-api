using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks;

namespace TaskManager.Api.Controllers;

/// <remarks>
/// Errors (validation, not found) are thrown by the service and turned into
/// ProblemDetails by GlobalExceptionHandler, so actions only handle the happy path.
/// </remarks>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(ITaskService taskService) : ControllerBase
{
    /// <summary>Lists tasks with optional filters, search, sorting and pagination.</summary>
    /// <remarks>Example: <c>GET /api/tasks?status=Pending&amp;search=report&amp;sortBy=DueDate&amp;sortDirection=Asc&amp;page=1&amp;pageSize=20</c></remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetAll([FromQuery] TaskQueryParameters query, CancellationToken ct) =>
        Ok(await taskService.GetPagedAsync(query, ct));

    /// <summary>Counts tasks by status, plus overdue tasks and completion rate.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<TaskSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct) =>
        Ok(await taskService.GetSummaryAsync(ct));

    /// <summary>Gets a single task by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await taskService.GetByIdAsync(id, ct));

    /// <summary>Creates a new task.</summary>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await taskService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>Replaces a task's editable fields.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken ct) =>
        Ok(await taskService.UpdateAsync(id, request, ct));

    /// <summary>Changes only the status of a task.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken ct) =>
        Ok(await taskService.UpdateStatusAsync(id, request, ct));

    /// <summary>Deletes a task.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await taskService.DeleteAsync(id, ct);
        return NoContent();
    }
}
