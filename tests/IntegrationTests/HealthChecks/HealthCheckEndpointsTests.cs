using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.HealthChecks;

public class HealthCheckEndpointsTests : IClassFixture<AutenticacaoWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AutenticacaoWebApplicationFactory _factory;

    public HealthCheckEndpointsTests(AutenticacaoWebApplicationFactory factory) => _factory = factory;

    [Theory(DisplayName = "GET /healthz* — retorna 200 Healthy sem autenticação e sem checks de dependências")]
    [InlineData("/healthz")]
    [InlineData("/healthz/live")]
    [InlineData("/healthz/ready")]
    public async Task Healthz_DeveRetornar200_SemAutenticacaoESemChecksDeDependencia(string rota)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(rota, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthReportDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Empty(body.Checks);
    }

    private sealed record HealthReportDto(string Status, List<HealthCheckEntryDto> Checks);

    private sealed record HealthCheckEntryDto(string Name, string Status);
}
