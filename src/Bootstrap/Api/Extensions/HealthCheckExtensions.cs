using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Api.Extensions;

/// <summary>
/// Configura health checks separando liveness (processo vivo, sem dependências) de
/// readiness (apto a atender tráfego). O serviço de autenticação não possui banco nem outra
/// dependência externa, então a readiness reflete apenas o próprio processo.
/// </summary>
public static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }

    public static IEndpointRouteBuilder MapApiHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness: só confirma que o processo está de pé, sem checar dependências externas.
        endpoints.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponseAsync
        }).AllowAnonymous();

        // Readiness: apto a atender tráfego; inclui eventuais checks marcados com a tag "ready"
        // (hoje não há nenhum — o processo responder já indica que está pronto).
        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteResponseAsync
        }).AllowAnonymous();

        // Mantido por compatibilidade com o smoke test da pipeline: agrega todos os checks.
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = WriteResponseAsync
        }).AllowAnonymous();

        return endpoints;
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
