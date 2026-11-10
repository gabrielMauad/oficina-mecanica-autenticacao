namespace Autenticacao.Application.Services;

public sealed record TokenInfo(string Token, DateTime ExpiresAt);
