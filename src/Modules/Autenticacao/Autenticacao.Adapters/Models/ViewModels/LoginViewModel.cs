namespace Autenticacao.Adapters.Models.ViewModels;

public sealed record LoginViewModel(string Token, DateTime ExpiresAt);
