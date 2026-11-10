namespace Autenticacao.Application.Options;

public sealed class AdminUserOptions
{
    public const string SectionName = "Auth";

    public string AdminEmail { get; set; } = string.Empty;
    public string AdminSenha { get; set; } = string.Empty;
}
