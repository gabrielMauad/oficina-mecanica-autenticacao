using Autenticacao.Adapters.Models.Request;
using Autenticacao.Adapters.Models.ViewModels;
using Autenticacao.Adapters.Presenters;
using Autenticacao.Application.Commands.Login;
using MediatR;
using SharedKernel.Domain;

namespace Autenticacao.Adapters.Controllers;

public sealed class AutenticacaoController
{
    private readonly ISender _sender;

    public AutenticacaoController(ISender sender) => _sender = sender;

    public async Task<Result<LoginViewModel>> Login(LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Senha);
        var result = await _sender.Send(command);
        if (result.IsFailure) return result.Error;
        return AutenticacaoPresenter.PresentLogin(result.Value);
    }
}
