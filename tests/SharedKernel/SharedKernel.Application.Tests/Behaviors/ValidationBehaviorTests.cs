using FluentValidation;
using MediatR;
using SharedKernel.Application.Behaviors;
using SharedKernel.Domain;

namespace SharedKernel.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record Comando(string Nome) : IRequest<Result<string>>;

    private sealed record ComandoSemResult(string Nome) : IRequest<string>;

    private sealed class NomeObrigatorio : AbstractValidator<Comando>
    {
        public NomeObrigatorio() => RuleFor(c => c.Nome).NotEmpty().WithMessage("Nome é obrigatório.");
    }

    private sealed class NomeComLimite : AbstractValidator<Comando>
    {
        public NomeComLimite() => RuleFor(c => c.Nome).MaximumLength(3).WithMessage("Nome muito longo.");
    }

    private sealed class NomeObrigatorioSemResult : AbstractValidator<ComandoSemResult>
    {
        public NomeObrigatorioSemResult() => RuleFor(c => c.Nome).NotEmpty();
    }

    [Fact(DisplayName = "Sem validators — segue para o próximo handler")]
    public async Task SemValidators_DeveChamarNext()
    {
        var behavior = new ValidationBehavior<Comando, Result<string>>([]);

        var result = await behavior.Handle(new Comando(""), _ => Task.FromResult(Result.Success("ok")), default);

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "Request válido — segue para o próximo handler")]
    public async Task RequestValido_DeveChamarNext()
    {
        var behavior = new ValidationBehavior<Comando, Result<string>>([new NomeObrigatorio()]);

        var result = await behavior.Handle(new Comando("ana"), _ => Task.FromResult(Result.Success("ok")), default);

        Assert.Equal("ok", result.Value);
    }

    [Fact(DisplayName = "Request inválido — devolve Result de falha agregando as mensagens, sem chamar next")]
    public async Task RequestInvalido_DeveRetornarFalhaComMensagensAgregadas()
    {
        var chamouNext = false;
        var behavior = new ValidationBehavior<Comando, Result<string>>([new NomeObrigatorio()]);

        var result = await behavior.Handle(
            new Comando(""),
            _ => { chamouNext = true; return Task.FromResult(Result.Success("ok")); },
            default);

        Assert.False(chamouNext);
        Assert.True(result.IsFailure);
        Assert.Equal("validation.failed", result.Error.Code);
        Assert.Equal("Nome é obrigatório.", result.Error.Message);

        var comDoisValidators = new ValidationBehavior<Comando, Result<string>>([new NomeObrigatorio(), new NomeComLimite()]);
        var resultMultiplo = await comDoisValidators.Handle(
            new Comando("nome-longo"),
            _ => Task.FromResult(Result.Success("ok")),
            default);
        Assert.Equal("Nome muito longo.", resultMultiplo.Error.Message);
    }

    [Fact(DisplayName = "Resposta que não é Result<T> — segue para o próximo handler mesmo inválido")]
    public async Task RespostaSemResult_DeveChamarNext()
    {
        var behavior = new ValidationBehavior<ComandoSemResult, string>([new NomeObrigatorioSemResult()]);

        var resposta = await behavior.Handle(new ComandoSemResult(""), _ => Task.FromResult("seguiu"), default);

        Assert.Equal("seguiu", resposta);
    }
}
