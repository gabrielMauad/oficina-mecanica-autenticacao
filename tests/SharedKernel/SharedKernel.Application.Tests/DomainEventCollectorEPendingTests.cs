using SharedKernel.Domain;

namespace SharedKernel.Application.Tests;

public class DomainEventCollectorEPendingTests
{
    private sealed record EventoDominio : IDomainEvent;

    private sealed class Agregado : AggregateRoot<Guid>
    {
        public Agregado() : base(Guid.NewGuid()) { }
        public void Disparar() => AddDomainEvent(new EventoDominio());
    }

    [Fact(DisplayName = "DomainEventCollector — não duplica agregado, coleta eventos e limpa tudo")]
    public void Collector_DeveRegistrarSemDuplicarColetarELimpar()
    {
        var collector = new DomainEventCollector();
        var agregado = new Agregado();
        agregado.Disparar();

        collector.Registrar(agregado);
        collector.Registrar(agregado);

        Assert.Single(collector.Coletar());

        collector.Limpar();

        Assert.Empty(agregado.DomainEvents);
        Assert.Empty(collector.Coletar());
    }

    [Fact(DisplayName = "PendingIntegrationEvents — GetPending devolve a fila e a esvazia")]
    public async Task Pending_GetPending_DeveDevolverEEsvaziar()
    {
        var pending = new PendingIntegrationEvents();
        var chamadas = 0;
        pending.Enqueue(_ => { chamadas++; return Task.CompletedTask; });
        pending.Enqueue(_ => { chamadas++; return Task.CompletedTask; });

        var primeira = pending.GetPending();
        foreach (var publish in primeira)
            await publish(default);

        Assert.Equal(2, chamadas);
        Assert.Empty(pending.GetPending());
    }
}
