using Api.Extensions;
using Api.Middlewares;
using Api.OpenApi;
using Autenticacao.Infrastructure;
using Autenticacao.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using SharedKernel.Application;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredJsonLogging();
builder.AddOtlpLogging();
builder.AddOficinaMecanicaObservability();

builder.Services.AddOficinaMecanicaAuthentication(builder.Configuration);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        var hasAllowAnonymous = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer"),
                        new List<string>()
                    }
                }
            ];
        }

        return Task.CompletedTask;
    });
});


builder.Services.AddApiHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddAutenticacaoModule(builder.Configuration);
builder.Services.AddSharedKernelServices();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(AutenticacaoAssemblyMarker).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapOpenApiDocumentation();
app.MapApiHealthChecks();


await app.RunAsync();

// Necessário para o WebApplicationFactory<Program> nos testes de integração
public partial class Program { }
