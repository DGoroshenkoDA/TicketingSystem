using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Ticketing.Tests;

// Phase 0/1 smoke test: the API boots and /health responds 200.
// Runs in the "Testing" environment so startup migrations are skipped
// (no database is required for this test).
public class HealthEndpointTests : IClassFixture<HealthEndpointTests.TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public HealthEndpointTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Returns_Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public class TestAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // A syntactically valid connection string; never actually connected to.
                    ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test"
                });
            });
        }
    }
}
