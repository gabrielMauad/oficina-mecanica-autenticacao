using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Auth;

public class LoginEndpointTests : IClassFixture<AutenticacaoWebApplicationFactory>
{
    private readonly AutenticacaoWebApplicationFactory _factory;

    public LoginEndpointTests(AutenticacaoWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "POST /api/v1/auth/login — credencial válida retorna 200 com token iss/aud/role=Oficina")]
    public async Task Login_DeveRetornarTokenComClaimsEsperadas_QuandoCredencialValida()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AutenticacaoWebApplicationFactory.AdminEmail,
            senha = AutenticacaoWebApplicationFactory.AdminSenha
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Token);
        Assert.Equal(AutenticacaoWebApplicationFactory.Issuer, jwt.Issuer);
        Assert.Contains(AutenticacaoWebApplicationFactory.Audience, jwt.Audiences);
        Assert.Equal("Oficina", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal(AutenticacaoWebApplicationFactory.AdminEmail, jwt.Subject);
    }

    [Theory(DisplayName = "POST /api/v1/auth/login — credencial inválida retorna 401")]
    [InlineData("admin@integration.test", "senha-errada")]
    [InlineData("outro@integration.test", "Test@Integration1234")]
    public async Task Login_DeveRetornar401_QuandoCredencialInvalida(string email, string senha)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record LoginResponse(string Token, DateTime ExpiresAt);
}
