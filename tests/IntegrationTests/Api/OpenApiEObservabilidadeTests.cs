using System.Net;
using System.Text.Json;
using Api.Extensions;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace IntegrationTests.Api;

public class OpenApiEObservabilidadeTests : IClassFixture<AutenticacaoWebApplicationFactory>
{
    private readonly AutenticacaoWebApplicationFactory _factory;

    public OpenApiEObservabilidadeTests(AutenticacaoWebApplicationFactory factory) => _factory = factory;

    private HttpClient CriarClienteComOpenApi(bool habilitado) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["OpenApi:Enabled"] = habilitado.ToString() })))
            .CreateClient(new() { AllowAutoRedirect = false });

    [Fact(DisplayName = "OpenApi habilitado — documento declara o esquema de segurança Bearer (JWT)")]
    public async Task OpenApiHabilitado_DocumentoDeveDeclararEsquemaBearer()
    {
        using var client = CriarClienteComOpenApi(habilitado: true);

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var bearer = doc.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact(DisplayName = "OpenApi habilitado — endpoints anônimos de autenticação não declaram requisito de segurança")]
    public async Task OpenApiHabilitado_OperacoesAnonimasNaoDeclaramSeguranca()
    {
        using var client = CriarClienteComOpenApi(habilitado: true);

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var doc = JsonDocument.Parse(json);
        var operacoes = doc.RootElement.GetProperty("paths").EnumerateObject()
            .Where(p => p.Name.StartsWith("/api/v1/auth"))
            .SelectMany(p => p.Value.EnumerateObject())
            .Select(op => op.Value)
            .ToList();
        Assert.NotEmpty(operacoes);
        Assert.All(operacoes, op => Assert.False(op.TryGetProperty("security", out _)));
    }

    [Fact(DisplayName = "OpenApi habilitado — raiz redireciona para a UI do Scalar")]
    public async Task OpenApiHabilitado_RaizDeveRedirecionarParaScalar()
    {
        using var client = CriarClienteComOpenApi(habilitado: true);

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/scalar", response.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "OpenApi desabilitado — documento não é exposto")]
    public async Task OpenApiDesabilitado_DocumentoNaoDeveSerExposto()
    {
        using var client = CriarClienteComOpenApi(habilitado: false);

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "Observabilidade — fora do ambiente Testing registra os provedores de traces e métricas")]
    public void Observabilidade_ForaDeTesting_DeveRegistrarProvedores()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });

        builder.AddOficinaMecanicaObservability();
        builder.AddOtlpLogging();
        builder.AddStructuredJsonLogging();

        using var provider = builder.Services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<TracerProvider>());
        Assert.NotNull(provider.GetService<MeterProvider>());
        Assert.NotNull(provider.GetService<ILoggerFactory>());
    }

    [Fact(DisplayName = "Observabilidade — no ambiente Testing o SDK do OpenTelemetry fica fora do host")]
    public void Observabilidade_EmTesting_NaoDeveRegistrarProvedores()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });

        builder.AddOficinaMecanicaObservability();
        builder.AddOtlpLogging();

        using var provider = builder.Services.BuildServiceProvider();
        Assert.Null(provider.GetService<TracerProvider>());
        Assert.Null(provider.GetService<MeterProvider>());
    }
}
