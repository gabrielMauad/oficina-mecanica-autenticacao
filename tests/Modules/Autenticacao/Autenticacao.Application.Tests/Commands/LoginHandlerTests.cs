using Autenticacao.Application.Commands.Login;
using Autenticacao.Application.Options;
using Autenticacao.Application.Services;
using Microsoft.Extensions.Options;
using Moq;
using SharedKernel.Domain;

namespace Autenticacao.Application.Tests.Commands;

public class LoginHandlerTests
{
    private readonly AdminUserOptions _options;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _options = new AdminUserOptions
        {
            AdminEmail = "admin@oficina.com",
            AdminSenha = "admin123"
        };

        _tokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new LoginHandler(
            Microsoft.Extensions.Options.Options.Create(_options),
            _tokenServiceMock.Object);
    }

    [Fact(DisplayName = "Cenário feliz: credenciais corretas retornam token")]
    public async Task Handle_ShouldReturnToken_WhenCredenciaisCorretas()
    {
        // Arrange
        var command = new LoginCommand("admin@oficina.com", "admin123");
        var tokenInfo = new TokenInfo("jwt-token-gerado", DateTime.UtcNow.AddHours(1));

        _tokenServiceMock
            .Setup(x => x.Gerar(command.Email, "Oficina"))
            .Returns(tokenInfo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
        Assert.Equal(tokenInfo.Token, result.Value.Token);
        Assert.Equal(tokenInfo.ExpiresAt, result.Value.ExpiresAt);

        _tokenServiceMock.Verify(x => x.Gerar(command.Email, "Oficina"), Times.Once);
    }

    [Fact(DisplayName = "Email case-insensitive: maiúsculas são aceitas")]
    public async Task Handle_ShouldReturnToken_WhenEmailEmMaiusculas()
    {
        // Arrange
        var command = new LoginCommand("ADMIN@OFICINA.COM", "admin123");
        var tokenInfo = new TokenInfo("jwt-token-gerado", DateTime.UtcNow.AddHours(1));

        _tokenServiceMock
            .Setup(x => x.Gerar(command.Email, "Oficina"))
            .Returns(tokenInfo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);

        _tokenServiceMock.Verify(x => x.Gerar(command.Email, "Oficina"), Times.Once);
    }

    [Fact(DisplayName = "Erro: email incorreto retorna CredenciaisInvalidas")]
    public async Task Handle_ShouldReturnError_WhenEmailIncorreto()
    {
        // Arrange
        var command = new LoginCommand("outro@email.com", "admin123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Autenticacao.CredenciaisInvalidas", result.Error.Code);
        Assert.Equal("Email ou senha inválidos.", result.Error.Message);

        _tokenServiceMock.Verify(x => x.Gerar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Erro: senha incorreta retorna CredenciaisInvalidas")]
    public async Task Handle_ShouldReturnError_WhenSenhaIncorreta()
    {
        // Arrange
        var command = new LoginCommand("admin@oficina.com", "senha-errada");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Autenticacao.CredenciaisInvalidas", result.Error.Code);
        Assert.Equal("Email ou senha inválidos.", result.Error.Message);

        _tokenServiceMock.Verify(x => x.Gerar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Erro: email e senha ambos incorretos retornam CredenciaisInvalidas")]
    public async Task Handle_ShouldReturnError_WhenAmbosIncorretos()
    {
        // Arrange
        var command = new LoginCommand("outro@email.com", "senha-errada");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Autenticacao.CredenciaisInvalidas", result.Error.Code);

        _tokenServiceMock.Verify(x => x.Gerar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Token não é gerado quando credenciais são inválidas")]
    public async Task Handle_ShouldNeverCallTokenService_WhenCredenciaisInvalidas()
    {
        // Arrange
        var commandEmailErrado = new LoginCommand("errado@email.com", "admin123");
        var commandSenhaErrada = new LoginCommand("admin@oficina.com", "senhaerrada");

        // Act
        await _handler.Handle(commandEmailErrado, CancellationToken.None);
        await _handler.Handle(commandSenhaErrada, CancellationToken.None);

        // Assert — IJwtTokenService nunca deve ser chamado para credenciais inválidas
        _tokenServiceMock.Verify(x => x.Gerar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
