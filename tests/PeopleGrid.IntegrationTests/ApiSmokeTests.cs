using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PeopleGrid.IntegrationTests;

public sealed class ApiSmokeTests
{
    [Fact(Skip = "Requires test database/tenant configuration.")]
    public async Task Swagger_ShouldBeAvailable()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
