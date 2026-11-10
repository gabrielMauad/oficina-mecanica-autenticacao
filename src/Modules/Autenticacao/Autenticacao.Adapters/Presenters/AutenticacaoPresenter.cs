using Autenticacao.Adapters.Models.ViewModels;
using Autenticacao.Application.Services;

namespace Autenticacao.Adapters.Presenters;

public static class AutenticacaoPresenter
{
    public static LoginViewModel PresentLogin(TokenInfo tokenInfo) =>
        new(tokenInfo.Token, tokenInfo.ExpiresAt);
}
