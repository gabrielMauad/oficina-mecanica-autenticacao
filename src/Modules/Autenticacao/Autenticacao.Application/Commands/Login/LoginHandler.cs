using Autenticacao.Application.Errors;
using Autenticacao.Application.Options;
using Autenticacao.Application.Services;
using MediatR;
using Microsoft.Extensions.Options;
using SharedKernel.Domain;

namespace Autenticacao.Application.Commands.Login;

public sealed class LoginHandler : IRequestHandler<LoginCommand, Result<TokenInfo>>
{
    private readonly AdminUserOptions _adminOptions;
    private readonly IJwtTokenService _tokenService;

    public LoginHandler(IOptions<AdminUserOptions> adminOptions, IJwtTokenService tokenService)
    {
        _adminOptions = adminOptions.Value;
        _tokenService = tokenService;
    }

    public Task<Result<TokenInfo>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var emailValido = command.Email.Equals(_adminOptions.AdminEmail, StringComparison.OrdinalIgnoreCase);
        var senhaValida = command.Senha == _adminOptions.AdminSenha;

        if (!emailValido || !senhaValida)
            return Task.FromResult<Result<TokenInfo>>(AutenticacaoErrors.CredenciaisInvalidas);

        var tokenInfo = _tokenService.Gerar(command.Email, "Oficina");
        return Task.FromResult<Result<TokenInfo>>(tokenInfo);
    }
}
