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
    public async Tas