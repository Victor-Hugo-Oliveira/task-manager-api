using System.Net;

namespace TaskManager.IntegrationTests;

public class HealthEndpointTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    [Fact]
    public async Task Health_endpoint_is_public_and_reports_healthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
