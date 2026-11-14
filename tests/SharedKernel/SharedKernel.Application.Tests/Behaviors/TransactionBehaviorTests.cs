using MediatR;
using SharedKernel.Application.Behaviors;
using SharedKernel.Domain;

namespace SharedKernel.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private sealed record Comando : ICommand<string>;

    private sealed record EventoDominio : IDomainEvent;

    private sealed class Agregado : AggregateRoot<Guid>
    {
        public Agregado() : base(Guid.NewGuid()) { }
        public void Disparar() => AddDomainEvent(new EventoDominio());
    }

    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly DomainEventCollector _collector = new();
    private readonly PendingIntegrationEvents _pending = new();

    private TransactionBehavior<Comando, Result<string>> CriarBehavior() =>
        new([_uow.Object], _collector, _pending, _publisher.Object);

    [Fact(DisplayName = "Sucesso — persiste, publica eventos de domínio e executa os eventos de integração pendentes")]
    public async Task Sucesso_DevePersistirEPublicar()
    {
        var agregado = new Agregado();
        agregado.Disparar();
        _collector.Registrar(agregado);
        var integracaoExecutada = false;
        _pending.Enqueue(_ => { integracaoExecutada = true; return Task.CompletedTask; });

        var result = await CriarBehavior().Handle(new Comando(), _ => Task.FromResult(Result.Success("ok")), default);

        Assert.True(result.IsSuccess);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(integracaoExecutada);
        Assert.Empty(agregado.DomainEvents);
    }

    [Fact(DisplayName = "Falha — não persiste nem publica nada")]
    public async Task Falha_NaoDevePersistirNemPublicar()
    {
        var agregado = new Agregado();
        agregado.Disparar();
        _collector.Registrar(agregado);

        var result = await CriarBehavior().Handle(
            new Comando(),
            _ => Task.FromResult(Result.Failure<string>(Error.Validation("x", "falhou"))),
            default);

        Assert.True(result.IsFailure);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
