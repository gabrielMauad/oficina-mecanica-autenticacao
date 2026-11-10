using SharedKernel.Domain;

namespace Autenticacao.Application.Errors;

public static class AutenticacaoErrors
{
    public static readonly Error CredenciaisInvalidas =
        new("Autenticacao.CredenciaisInvalidas", "Email ou senha inválidos.");
}
