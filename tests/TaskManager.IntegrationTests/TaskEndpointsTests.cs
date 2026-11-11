using System.Net;
using System.Net.Http.Json;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Enums;
using static TaskManager.IntegrationTests.ApiTestHelpers;

namespace TaskManager.IntegrationTests;

public class TaskEndpointsTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private static readonly DateTime NextMonth = DateTime.UtcNow.AddDays(30);

    [Fact]
    public async Task Full_crud_flow()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        // Create
        var create = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("Write tests", "integration", TaskPriority.High, NextMonth), Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.ReadAsync<TaskResponse>();
        Assert.Equal($"/api/tasks/{created.Id}", create.Headers.Location?.AbsolutePath);

        // Read
        var fetched = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}", Json);
        Assert.Equal("Write tests", fetched!.Title);
        Assert.Equal(DateTimeKind.Utc, fetched.CreatedAt.Kind);

        // Update
        var update = await client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Write more tests", null, TaskPriority.Low, null), Json);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Write more tests", (await update.ReadAsync<TaskResponse>()).Title);

        // Change status
        var patch = await client.PatchAsJsonAsync($"/api/tasks/{created.Id}/status",
            new UpdateTaskStatusRequest(TaskItemStatus.Done), Json);
        var done = await patch.ReadAsync<TaskResponse>();
        Assert.Equal(TaskItemStatus.Done, done.Status);
        Assert.NotNull(done.CompletedAt);

        // Delete
        var delete = await client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var afterDelete = await client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_data_returns_400_problem_details()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest("", null, TaskPriority.Low, DateTime.UtcNow.AddDays(-1)), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Title", out _));
        Assert.True(errors.TryGetProperty("DueDate", out _));
    }

    [Fact]
    public async Task Unknown_task_returns_404_problem_details()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.ReadProblemAsync()).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Users_cannot_see_or_change_each_others_tasks()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var aliceTask = await alice.CreateTaskAsync(new CreateTaskRequest("Alice's secret", null));
        var url = $"/api/tasks/{aliceTask.Id}";

        // Every operation on someone else's task looks like "not found" (404), never 403.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync(url,
            new UpdateTaskRequest("hacked", null, TaskPriority.Low, null), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PatchAsJsonAsync($"{url}/status",
            new UpdateTaskStatusRequest(TaskItemStatus.Done), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync(url)).StatusCode);

        var bobList = await bob.GetFromJsonAsync<PagedResponse<TaskResponse>>("/api/tasks", Json);
        Assert.Equal(0, bobList!.TotalCount);

        // Alice's task is untouched.
        var stillThere = await alice.GetFromJsonAsync<TaskResponse>(url, Json);
        Assert.Equal("Alice's secret", stillThere!.Title);
        Assert.Equal(TaskItemStatus.Pending, stillThere.Status);
    }

    [Fact]
    public async Task List_supports_filtering_search_sorting_and_paging()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.CreateTaskAsync(new CreateTaskRequest("Low task", null, TaskPriority.Low));
        await client.CreateTaskAsync(new CreateTaskRequest("High task", "quarterly REPORT", TaskPriority.High));
        await client.CreateTaskAsync(new CreateTaskRequest("Medium task", null, TaskPriority.Medium));
        var done = await client.CreateTaskAsync(new CreateTaskRequest("Done task", null, TaskPriority.Low));
        await client.PatchAsJsonAsync($"/api/tasks/{done.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done), Json);

        async Task<PagedResponse<TaskResponse>> List(string query) =>
            (await client.GetFromJsonAsync<PagedResponse<TaskResponse>>($"/api/tasks{query}", Json))!;

        // Filter
        var pending = await List("?status=Pending");
        Assert.Equal(3, pending.TotalCount);
        Assert.All(pending.Items, t => Assert.Equal(TaskItemStatus.Pending, t.Status));

        // Search in description, case-insensitive
        var search = await List("?search=report");
        Assert.Equal("High task", Assert.Single(search.Items).Title);

        // Sort by priority meaning, not alphabetically
        var byPriority = await List("?status=Pending&sortBy=Priority&sortDirection=Desc");
        Assert.Equal(["High task", "Medium task", "Low task"], byPriority.Items.Select(t => t.Title));

        // Paging
        var page2 = await List("?pageSize=3&page=2");
        Assert.Equal(4, page2.TotalCount);
        Assert.Single(page2.Items);
        Assert.True(page2.HasPreviousPage);
        Assert.False(page2.HasNextPage);
    }

    [Fact]
    public async Task Invalid_query_parameters_return_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tasks?page=0&pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Page", out _));
        Assert.True(errors.TryGetProperty("PageSize", out _));
    }

    [Fact]
    public async Task Summary_counts_only_the_current_users_tasks()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var other = await factory.CreateAuthenticatedClientAsync();
        await other.CreateTaskAsync(new CreateTaskRequest("Not mine", null));

        await client.CreateTaskAsync(new CreateTaskRequest("A", null));
        var b = await client.CreateTaskAsync(new CreateTaskRequest("B", null));
        await client.PatchAsJsonAsync($"/api/tasks/{b.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done), Json);

        var summary = await client.GetFromJsonAsync<TaskSummaryResponse>("/api/tasks/summary", Json);

        Assert.Equal(new TaskSummaryResponse(Total: 2, Pending: 1, InProgress: 0, Done: 1, Overdue: 0, CompletionRate: 50.0), summary);
    }
}
