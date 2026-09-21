using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DMO.IntegrationTests.Host;
using DMO.Web.Endpoints;

namespace DMO.IntegrationTests;

/// <summary>
/// Proposed P1-T01 test — the technical startup endpoint answers.
/// </summary>
/// <remarks>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </remarks>
public sealed class TechnicalEndpointTests : IClassFixture<DmoWebApplicationFactory>
{
    private readonly DmoWebApplicationFactory _factory;

    public TechnicalEndpointTests(DmoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOkWithStatusPayload()
    {
        // Preconditions: the host started with database configuration supplied.
        using var client = _factory.CreateClient();

        // Action: request the technical startup endpoint.
        var response = await client.GetAsync(TechnicalEndpoints.HealthPath);

        // Assertions: reachable and reports liveness.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", payload.GetProperty("status").GetString());

        // Required non-effect: no account/Template/Module/industrial data is exposed.
        Assert.False(payload.TryGetProperty("user", out _));
        Assert.False(payload.TryGetProperty("modules", out _));
        Assert.False(payload.TryGetProperty("templates", out _));
    }

    [Fact]
    public async Task Health_DoesNotExposeTheConnectionString()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(TechnicalEndpoints.HealthPath);
        var body = await response.Content.ReadAsStringAsync();

        // Required non-effect: no credential material leaks through the technical surface.
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dmo_placeholder", body, StringComparison.OrdinalIgnoreCase);
    }
}
