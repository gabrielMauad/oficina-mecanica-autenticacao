using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Factory mínima do serviço de autenticação: não há banco nem dependência externa, então basta
/// fornecer a configuração de JWT e do usuário administrador.
/// </summary>
public sealed class AutenticacaoWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string JwtSecret = "integration-test-jwt-secret-minimum-32-chars!!";
    public const string Issuer = "oficina-mecanica-app";
    public const string Audience = "oficina-mecanica-api";
    public const string AdminEmail = "admin@integration.test";
    public const string AdminSenha = "Test@Integration1234";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ValidIssuers:0"] = Issuer,
                ["Jwt:ValidIssuers:1"] = "oficina-mecanica-auth",
                ["Auth:AdminEmail"] = AdminEmail,
                ["Auth:AdminSenha"] = AdminSenha,
                ["OpenApi:Enabled"] = "false",
            });
        });
    }
}
