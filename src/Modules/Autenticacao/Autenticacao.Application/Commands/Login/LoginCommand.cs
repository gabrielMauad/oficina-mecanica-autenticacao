using Autenticacao.Application.Services;
using SharedKernel.Application;

namespace Autenticacao.Application.Commands.Login;

public sealed record LoginCommand(string Email, string Senha) : ICommand<TokenInfo>;
