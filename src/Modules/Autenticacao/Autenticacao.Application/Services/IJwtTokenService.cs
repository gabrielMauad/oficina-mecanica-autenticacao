namespace Autenticacao.Application.Services;

public interface IJwtTokenService
{
    TokenInfo Gerar(string email, string role);
}
