using SharedKernel.Domain;

namespace SharedKernel.Application.Tests.Domain;

public class ResultETests
{
    [Fact(DisplayName = "Result.Success — expõe o valor e não carrega erro")]
    public void Success_DeveExporValorESemErro()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact(DisplayName = "Result.Failure — carrega o erro e lança ao acessar Value")]
    public void Failure_DeveCarregarErroELancarAoAcessarValue()
    {
        var erro = Error.NotFound("x.nao_encontrado", "Não encontrado.");

        var result = Result.Failure<int>(erro);

        Assert.True(result.IsFailure);
        Assert.Equal(erro, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact(DisplayName = "Conversões implícitas — valor vira sucesso e Error vira falha")]
    public void ConversoesImplicitas_DevemProduzirSucessoEFalha()
    {
        Result<string> sucesso = "ok";
        Result<string> falha = Error.Conflict("x.conflito", "Conflito.");

        Assert.True(sucesso.IsSuccess);
        Assert.Equal("ok", sucesso.Value);
        Assert.True(falha.IsFailure);
        Assert.Equal("x.conflito", falha.Error.Code);
    }

    [Fact(DisplayName = "Error — fábricas preservam código e mensagem")]
    public void Error_Fabricas_DevemPreservarCodigoEMensagem()
    {
        Assert.Equal(new Error("a", "b"), Error.Validation("a", "b"));
        Assert.Equal(new Error("a", "b"), Error.NotFound("a", "b"));
        Assert.Equal(new Error("a", "b"), Error.Conflict("a", "b"));
        Assert.Equal(new Error("a", "b"), Error.Forbidden("a", "b"));
    }
}
